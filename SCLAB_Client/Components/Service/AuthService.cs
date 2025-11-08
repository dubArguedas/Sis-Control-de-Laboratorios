using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Blazored.LocalStorage;
using Microsoft.Extensions.Configuration;
using System.Net;
using SCLAB_Client.Models; // Agregar esta línea

namespace SCLAB_Client.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(string correo, string password);
        Task LogoutAsync();
        Task<bool> IsAuthenticatedAsync();
        Task<string?> GetTokenAsync();
        Task<UsuarioInfo?> GetCurrentUserInfoAsync();
        Task<BloqueoInfo> GetBloqueoInfoAsync(string correo);
        Task ResetFailedAttemptsAsync(string correo);
        Task<bool> ValidarCorreoExisteAsync(string correo);
        Task<bool> ValidarFormatoCorreoAsync(string correo);
    }

    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private readonly IConfiguration _configuration;

        public AuthService(HttpClient httpClient, ILocalStorageService localStorage, IConfiguration configuration)
        {
            // En lugar de usar _httpClient directamente, usa uno nombrado
            _httpClient = httpClient;
            _localStorage = localStorage;
            _configuration = configuration;

            // Opcional: configurar base address aquí si es necesario
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri("https://localhost:7241/");
            }
        }

        public async Task<LoginResponse> LoginAsync(string correo, string password)
        {
            try
            {
                // Normalizar correo
                correo = correo.Trim().ToLowerInvariant();

                // 1. Validar formato del correo
                if (!await ValidarFormatoCorreoAsync(correo))
                {
                    return new LoginResponse
                    {
                        Message = "El formato del correo institucional no es válido",
                        Token = "",
                        IsBlocked = false,
                        ErrorType = "INVALID_FORMAT"
                    };
                }

                // 2. Verificar si el correo está bloqueado ANTES de hacer cualquier petición
                var bloqueoInfo = await GetBloqueoInfoAsync(correo);
                if (bloqueoInfo.IsBlocked)
                {
                    return new LoginResponse
                    {
                        Message = $"Cuenta bloqueada temporalmente por seguridad",
                        Token = "",
                        IsBlocked = true,
                        TimeRemaining = bloqueoInfo.TimeRemaining,
                        RemainingAttempts = 0,
                        ErrorType = "BLOCKED"
                    };
                }

                // 3. Intentar login en la API REAL
                var loginDto = new LoginDto
                {
                    CorreoInstitucional = correo,
                    Password = password
                };

                var response = await _httpClient.PostAsJsonAsync("api/Usuarios/login", loginDto);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiLoginResponse>();

                    if (result != null && !string.IsNullOrEmpty(result.Token))
                    {
                        // Guardar token en localStorage
                        await _localStorage.SetItemAsync("authToken", result.Token);

                        // Resetear intentos fallidos en caso de éxito
                        await ResetFailedAttemptsAsync(correo);

                        // Extraer información del usuario del token
                        var userInfo = DecodeToken(result.Token);
                        if (userInfo != null)
                        {
                            await _localStorage.SetItemAsync("userInfo", userInfo);
                        }

                        return new LoginResponse
                        {
                            Message = result.Message ?? "Inicio de sesión exitoso",
                            Token = result.Token,
                            IsBlocked = false,
                            ErrorType = "NONE"
                        };
                    }
                }

                // 4. Manejar error de autenticación (contraseña incorrecta)
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // Registrar intento fallido
                    await RegisterFailedAttempt(correo);

                    // Verificar si ahora está bloqueado
                    var nuevoBloqueoInfo = await GetBloqueoInfoAsync(correo);

                    if (nuevoBloqueoInfo.IsBlocked)
                    {
                        return new LoginResponse
                        {
                            Message = "Demasiados intentos fallidos. Su cuenta ha sido bloqueada temporalmente por seguridad",
                            Token = "",
                            IsBlocked = true,
                            TimeRemaining = nuevoBloqueoInfo.TimeRemaining,
                            RemainingAttempts = 0,
                            ErrorType = "BLOCKED"
                        };
                    }

                    return new LoginResponse
                    {
                        Message = "La contraseña es incorrecta",
                        Token = "",
                        IsBlocked = false,
                        RemainingAttempts = nuevoBloqueoInfo.RemainingAttempts,
                        ErrorType = "WRONG_PASSWORD"
                    };
                }

                // 5. Otros errores del servidor
                var errorContent = await response.Content.ReadAsStringAsync();
                return new LoginResponse
                {
                    Message = "Error en el servidor. Intente nuevamente más tarde",
                    Token = "",
                    IsBlocked = false,
                    ErrorType = "SERVER_ERROR"
                };
            }
            catch (HttpRequestException ex)
            {
                return new LoginResponse
                {
                    Message = "Error de conexión con el servidor",
                    Token = "",
                    IsBlocked = false,
                    ErrorType = "CONNECTION_ERROR"
                };
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Message = $"Error inesperado: {ex.Message}",
                    Token = "",
                    IsBlocked = false,
                    ErrorType = "UNKNOWN_ERROR"
                };
            }
        }

        public class ApiLoginResponse
        {
            public string Token { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }

        public async Task<bool> ValidarFormatoCorreoAsync(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
                return false;

            // Validar formato básico de email
            if (!correo.Contains("@") || !correo.Contains("."))
                return false;

            // Validar que sea un correo institucional
            var dominiosValidos = new[] { "@est.univalle.edu", "@univalle.edu" };
            return dominiosValidos.Any(dominio => correo.EndsWith(dominio, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> ValidarCorreoExisteAsync(string correo)
        {
            try
            {
                correo = correo.Trim().ToLowerInvariant();

                // Intentar obtener la lista de usuarios (solo necesitamos verificar si existe el correo)
                var response = await _httpClient.GetAsync("api/Usuarios");

                if (response.IsSuccessStatusCode)
                {
                    var usuarios = await response.Content.ReadFromJsonAsync<List<UsuarioBasico>>();
                    if (usuarios != null)
                    {
                        return usuarios.Any(u => u.CorreoInstitucional.Trim().ToLowerInvariant() == correo);
                    }
                }

                // Si no podemos verificar, asumimos que existe para no bloquear el flujo
                return true;
            }
            catch
            {
                // En caso de error, asumimos que existe
                return true;
            }
        }

        public async Task<BloqueoInfo> GetBloqueoInfoAsync(string correo)
        {
            correo = correo.Trim().ToLowerInvariant();
            var attempts = await GetFailedAttempts(correo);
            var maxAttempts = _configuration.GetValue<int>("Security:MaxLoginAttempts", 3);
            var blockDuration = TimeSpan.FromMinutes(_configuration.GetValue<int>("Security:BlockDurationMinutes", 10));

            if (attempts.Count >= maxAttempts)
            {
                var timeElapsed = DateTime.UtcNow - attempts.LastAttempt;
                var timeRemaining = blockDuration - timeElapsed;

                if (timeRemaining > TimeSpan.Zero)
                {
                    return new BloqueoInfo
                    {
                        IsBlocked = true,
                        TimeRemaining = timeRemaining,
                        AttemptsCount = attempts.Count,
                        RemainingAttempts = 0
                    };
                }
                else
                {
                    // Tiempo de bloqueo expirado, resetear automáticamente
                    await ResetFailedAttemptsAsync(correo);
                }
            }

            return new BloqueoInfo
            {
                IsBlocked = false,
                TimeRemaining = TimeSpan.Zero,
                AttemptsCount = attempts.Count,
                RemainingAttempts = Math.Max(0, maxAttempts - attempts.Count)
            };
        }

        public async Task ResetFailedAttemptsAsync(string correo)
        {
            correo = correo.Trim().ToLowerInvariant();
            var key = $"failed_attempts_{correo}";
            await _localStorage.RemoveItemAsync(key);
        }

        private async Task<FailedAttempts> GetFailedAttempts(string correo)
        {
            correo = correo.Trim().ToLowerInvariant();
            var key = $"failed_attempts_{correo}";
            var attempts = await _localStorage.GetItemAsync<FailedAttempts>(key);

            if (attempts == null)
            {
                return new FailedAttempts { Count = 0, LastAttempt = DateTime.MinValue };
            }

            return attempts;
        }

        private async Task RegisterFailedAttempt(string correo)
        {
            correo = correo.Trim().ToLowerInvariant();
            var key = $"failed_attempts_{correo}";
            var attempts = await GetFailedAttempts(correo);

            attempts.Count++;
            attempts.LastAttempt = DateTime.UtcNow;

            await _localStorage.SetItemAsync(key, attempts);
        }

        public async Task LogoutAsync()
        {
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("userInfo");
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            return !string.IsNullOrEmpty(token);
        }

        public async Task<string?> GetTokenAsync()
        {
            return await _localStorage.GetItemAsync<string>("authToken");
        }

        public async Task<UsuarioInfo?> GetCurrentUserInfoAsync()
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token))
                return null;

            return DecodeToken(token);
        }

        private UsuarioInfo? DecodeToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
                var email = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
                var rol = jwtToken.Claims.FirstOrDefault(c => c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;

                if (userId != null && email != null && rol != null)
                {
                    return new UsuarioInfo
                    {
                        UsuarioId = int.Parse(userId),
                        CorreoInstitucional = email,
                        Rol = rol
                    };
                }
            }
            catch
            {
                return null;
            }

            return null;
        }
    }

    // Clases de modelo específicas del servicio
    public class FailedAttempts
    {
        public int Count { get; set; }
        public DateTime LastAttempt { get; set; }
    }

    public class BloqueoInfo
    {
        public bool IsBlocked { get; set; }
        public TimeSpan TimeRemaining { get; set; }
        public int AttemptsCount { get; set; }
        public int RemainingAttempts { get; set; }
    }
}