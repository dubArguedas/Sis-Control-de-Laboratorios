using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Blazored.LocalStorage;
using SCLAB_Client.Components.Service;

namespace SCLAB_Client.Models
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(string correo, string password);
        Task LogoutAsync();
        Task<bool> IsAuthenticatedAsync();
        Task<string?> GetTokenAsync();
        Task<UsuarioInfo?> GetCurrentUserInfoAsync();
    }

    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;

        public AuthService(HttpClient httpClient, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
        }

        public async Task<LoginResponse> LoginAsync(string correo, string password)
        {
            try
            {
                // Hashear la contraseña con SHA256 (igual que el backend)
                string passwordHash = HashPassword(password);

                // Crear el DTO
                var loginDto = new LoginDto
                {
                    CorreoInstitucional = correo,
                    PasswordHash = passwordHash
                };

                // Hacer la petición al API
                var response = await _httpClient.PostAsJsonAsync("api/Usuarios/login", loginDto);

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

                    if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.Token))
                    {
                        // Guardar el token en LocalStorage
                        await _localStorage.SetItemAsync("authToken", loginResponse.Token);

                        // Decodificar y guardar info del usuario
                        var userInfo = DecodeToken(loginResponse.Token);
                        if (userInfo != null)
                        {
                            await _localStorage.SetItemAsync("userRole", userInfo.Rol);
                            await _localStorage.SetItemAsync("userId", userInfo.UsuarioId);
                        }

                        return loginResponse;
                    }
                }

                // Si llegamos aquí, hubo un error
                var errorMessage = await response.Content.ReadAsStringAsync();
                return new LoginResponse
                {
                    Message = errorMessage.Contains("Credenciales") ? errorMessage : "Credenciales incorrectas",
                    Token = string.Empty
                };
            }
            catch (HttpRequestException ex)
            {
                return new LoginResponse
                {
                    Message = $"Error de conexión: {ex.Message}",
                    Token = string.Empty
                };
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Message = $"Error inesperado: {ex.Message}",
                    Token = string.Empty
                };
            }
        }

        public async Task LogoutAsync()
        {
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("userRole");
            await _localStorage.RemoveItemAsync("userId");
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

        // Hashear password con SHA256 (igual que el backend)
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Decodificar el token JWT para obtener información del usuario
        private UsuarioInfo? DecodeToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
                var email = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
                var rol = jwtToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

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
}