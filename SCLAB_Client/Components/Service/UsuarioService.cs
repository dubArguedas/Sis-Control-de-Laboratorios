using SCLAB_Client.Components.Service.ServiciosApi;
using SCLAB_Client.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace SCLAB_Client.Services
{
    public interface IUsuarioService
    {
        Task<List<UsuarioDto>> ListarUsuarios();
        Task<UsuarioDto?> ObtenerUsuarioPorId(int usuarioId);
        Task<string> CrearUsuario(UsuarioCreateDto usuario);
        Task<string> ActualizarUsuario(UsuarioUpdateDto usuario);
        Task<string> CambiarEstadoUsuario(int usuarioId);
        Task<string> ActivarUsuario(int usuarioId);
        Task<List<UsuarioDto>> ListarUsuariosPorRol(string rol);
        Task<bool> ValidarCorreoUnicoAsync(string correo);
        Task<bool> ValidarCIUnicoAsync(string ci);
    }

    public class UsuarioService : IUsuarioService
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenStateService _tokenState;

        public UsuarioService(HttpClient httpClient, ITokenStateService tokenState)
        {
            _httpClient = httpClient;
            _tokenState = tokenState;

            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri("https://localhost:7241/");
            }
        }

        private void AgregarTokenHeader()
        {
            var token = _tokenState.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            }
        }

        public async Task<List<UsuarioDto>> ListarUsuarios()
        {
            try
            {
                AgregarTokenHeader();

                var response = await _httpClient.GetAsync("api/Usuarios");

                if (response.IsSuccessStatusCode)
                {
                    var usuarios = await response.Content.ReadFromJsonAsync<List<UsuarioDto>>();
                    return usuarios ?? new List<UsuarioDto>();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("[UsuarioService] ❌ No autorizado para listar usuarios");
                    throw new UnauthorizedAccessException("No tiene permisos para realizar esta acción");
                }
                else
                {
                    Console.WriteLine($"[UsuarioService] ❌ Error al listar usuarios: {response.StatusCode}");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Error al listar usuarios: {response.StatusCode} - {errorContent}");
                }
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UsuarioService] ❌ Excepción al listar usuarios: {ex.Message}");
                throw new Exception("Error interno al listar usuarios", ex);
            }
        }

        public async Task<List<UsuarioDto>> ListarUsuariosPorRol(string rol)
        {
            try
            {
                AgregarTokenHeader();

                // Validar rol permitido
                var rolesPermitidos = new[] { "estudiante", "docente", "encargado", "admin" };
                if (!rolesPermitidos.Contains(rol.ToLower()))
                {
                    throw new ArgumentException($"Rol '{rol}' no es válido");
                }

                var response = await _httpClient.GetAsync($"api/Usuarios/{rol}");

                if (response.IsSuccessStatusCode)
                {
                    var usuarios = await response.Content.ReadFromJsonAsync<List<UsuarioDto>>();
                    return usuarios ?? new List<UsuarioDto>();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine($"[UsuarioService] ❌ No autorizado para listar usuarios de rol {rol}");
                    throw new UnauthorizedAccessException("No tiene permisos para realizar esta acción");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine($"[UsuarioService] ❌ No se encontraron usuarios para el rol {rol}");
                    return new List<UsuarioDto>();
                }
                else
                {
                    Console.WriteLine($"[UsuarioService] ❌ Error al listar usuarios por rol {rol}: {response.StatusCode}");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Error al listar usuarios: {response.StatusCode} - {errorContent}");
                }
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UsuarioService] ❌ Excepción al listar usuarios por rol {rol}: {ex.Message}");
                throw new Exception($"Error interno al listar usuarios del rol {rol}", ex);
            }
        }

        public async Task<UsuarioDto?> ObtenerUsuarioPorId(int usuarioId)
        {
            try
            {
                AgregarTokenHeader();

                if (usuarioId <= 0)
                {
                    throw new ArgumentException("ID de usuario no válido");
                }

                var response = await _httpClient.GetAsync($"api/Usuarios/{usuarioId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UsuarioDto>();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("[UsuarioService] ❌ No autorizado para obtener usuario");
                    throw new UnauthorizedAccessException("No tiene permisos para realizar esta acción");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine("[UsuarioService] ❌ Usuario no encontrado");
                    return null;
                }
                else
                {
                    Console.WriteLine($"[UsuarioService] ❌ Error al obtener usuario: {response.StatusCode}");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Error al obtener usuario: {response.StatusCode} - {errorContent}");
                }
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UsuarioService] ❌ Excepción al obtener usuario: {ex.Message}");
                throw new Exception("Error interno al obtener usuario", ex);
            }
        }

        public async Task<string> CrearUsuario(UsuarioCreateDto usuario)
        {
            try
            {
                AgregarTokenHeader();

                // Validaciones previas
                if (!await ValidarCorreoUnicoAsync(usuario.CorreoInstitucional))
                {
                    return "Error: El correo institucional ya existe";
                }

                if (!await ValidarCIUnicoAsync(usuario.CI))
                {
                    return "Error: El CI ya existe";
                }

                // Normalizar correo
                usuario.CorreoInstitucional = usuario.CorreoInstitucional.Trim().ToLowerInvariant();

                var response = await _httpClient.PostAsJsonAsync("api/Usuarios", usuario);

                if (response.IsSuccessStatusCode)
                {
                    return "Usuario creado exitosamente";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return "Error: No autorizado para crear usuarios";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<JsonElement>(errorContent);
                        if (errorObj.TryGetProperty("message", out var messageProp))
                        {
                            return $"Error: {messageProp.GetString()}";
                        }
                    }
                    catch
                    {
                        // Si no se puede deserializar, usar el contenido como está
                    }
                    return $"Error: {errorContent}";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"Error: {response.StatusCode} - {errorContent}";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> ActualizarUsuario(UsuarioUpdateDto usuario)
        {
            try
            {
                AgregarTokenHeader();

                if (usuario.UsuarioId <= 0)
                {
                    return "Error: ID de usuario no válido";
                }

                var response = await _httpClient.PutAsJsonAsync($"api/Usuarios/{usuario.UsuarioId}", usuario);

                if (response.IsSuccessStatusCode)
                {
                    return "Usuario actualizado exitosamente";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return "Error: No autorizado para actualizar usuarios";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return "Error: Usuario no encontrado";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<JsonElement>(errorContent);
                        if (errorObj.TryGetProperty("message", out var messageProp))
                        {
                            return $"Error: {messageProp.GetString()}";
                        }
                    }
                    catch
                    {
                        // Si no se puede deserializar, usar el contenido como está
                    }
                    return $"Error: {errorContent}";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"Error: {response.StatusCode} - {errorContent}";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> CambiarEstadoUsuario(int usuarioId)
        {
            try
            {
                AgregarTokenHeader();

                if (usuarioId <= 0)
                {
                    return "Error: ID de usuario no válido";
                }

                var response = await _httpClient.DeleteAsync($"api/Usuarios/{usuarioId}");

                if (response.IsSuccessStatusCode)
                {
                    return "Usuario desactivado exitosamente";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return "Error: No autorizado para cambiar estado de usuarios";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return "Error: Usuario no encontrado";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"Error: {response.StatusCode} - {errorContent}";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> ActivarUsuario(int usuarioId)
        {
            try
            {
                AgregarTokenHeader();

                if (usuarioId <= 0)
                {
                    return "Error: ID de usuario no válido";
                }

                var response = await _httpClient.PutAsync($"api/Usuarios/activo/{usuarioId}", null);

                if (response.IsSuccessStatusCode)
                {
                    return "Usuario activado exitosamente";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return "Error: No autorizado para activar usuarios";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return "Error: Usuario no encontrado";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"Error: {response.StatusCode} - {errorContent}";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<bool> ValidarCorreoUnicoAsync(string correo)
        {
            try
            {
                AgregarTokenHeader();

                var usuarios = await ListarUsuarios();
                return !usuarios.Any(u =>
                    u.CorreoInstitucional.Trim().ToLowerInvariant() == correo.Trim().ToLowerInvariant());
            }
            catch
            {
                // En caso de error, asumimos que el correo es único para no bloquear el flujo
                return true;
            }
        }

        public async Task<bool> ValidarCIUnicoAsync(string ci)
        {
            try
            {
                AgregarTokenHeader();

                var usuarios = await ListarUsuarios();
                return !usuarios.Any(u => u.CI == ci);
            }
            catch
            {
                // En caso de error, asumimos que el CI es único para no bloquear el flujo
                return true;
            }
        }
    }
}