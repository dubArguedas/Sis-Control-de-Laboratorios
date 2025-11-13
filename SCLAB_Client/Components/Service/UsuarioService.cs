using SCLAB_Client.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace SCLAB_Client.Services
{
    public class UsuarioService
    {
        private readonly HttpClient _httpClient;
        private readonly HttpClient _authHttpClient;
        private readonly IAuthService _authService;

        public UsuarioService(IHttpClientFactory httpClientFactory, IAuthService authService)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient"); // Sin token
            _authHttpClient = httpClientFactory.CreateClient("AuthApiClient"); // Con token
            _authService = authService;
        }

        // GET: Obtener todos los usuarios (público - sin token)
        public async Task<List<UsuarioDto>> ListarUsuarios()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/Usuarios");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    try
                    {
                        var usuarios = JsonSerializer.Deserialize<List<UsuarioDto>>(jsonString, options);
                        return usuarios ?? new List<UsuarioDto>();
                    }
                    catch (JsonException)
                    {
                        // Si falla, es porque viene en formato de objeto con listas separadas
                        var responseObj = JsonSerializer.Deserialize<UsuarioResponse>(jsonString, options);
                        var todosUsuarios = new List<UsuarioDto>();

                        if (responseObj?.UsuariosEstudiantes != null)
                            todosUsuarios.AddRange(responseObj.UsuariosEstudiantes);
                        if (responseObj?.UsuariosDocentes != null)
                            todosUsuarios.AddRange(responseObj.UsuariosDocentes);
                        if (responseObj?.UsuariosEncargado != null)
                            todosUsuarios.AddRange(responseObj.UsuariosEncargado);
                        if (responseObj?.UsuariosAdmin != null)
                            todosUsuarios.AddRange(responseObj.UsuariosAdmin);

                        return todosUsuarios;
                    }
                }

                return new List<UsuarioDto>();
            }
            catch
            {
                return new List<UsuarioDto>();
            }
        }

        // GET: Obtener usuario por ID (requiere token)
        public async Task<UsuarioDto> ObtenerUsuario(int id)
        {
            try
            {
                // Verificar si estamos autenticados antes de hacer la petición
                if (!await _authService.IsAuthenticatedAsync())
                {
                    throw new UnauthorizedAccessException("No autenticado");
                }

                var response = await _authHttpClient.GetFromJsonAsync<UsuarioDto>($"api/Usuarios/{id}");
                return response ?? new UsuarioDto();
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("No autorizado para ver este usuario");
            }
            catch
            {
                return new UsuarioDto();
            }
        }

        // POST: Crear usuario (requiere token)
        public async Task<string> CrearUsuario(UsuarioCreateDto oUsuarioCreateDto)
        {
            try
            {
                // Verificar autenticación
                if (!await _authService.IsAuthenticatedAsync())
                {
                    return "Error: No autenticado. Debe iniciar sesión para crear usuarios.";
                }

                var usuarioParaAPI = new
                {
                    Nombre = oUsuarioCreateDto.Nombre,
                    ApellidoPaterno = oUsuarioCreateDto.ApellidoPaterno,
                    ApellidoMaterno = oUsuarioCreateDto.ApellidoMaterno,
                    CorreoInstitucional = oUsuarioCreateDto.CorreoInstitucional.Trim().ToLowerInvariant(),
                    CI = oUsuarioCreateDto.CI,
                    Rol = oUsuarioCreateDto.Rol,
                    PasswordHash = oUsuarioCreateDto.PasswordHash
                };

                var response = await _authHttpClient.PostAsJsonAsync("api/Usuarios", usuarioParaAPI);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return "Error: No autorizado. Solo encargados y administradores pueden crear usuarios.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"Error: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        // PUT: Actualizar usuario (requiere token)
        public async Task<string> ActualizarUsuario(int id, UsuarioDto oUsuarioDto)
        {
            try
            {
                // Verificar autenticación
                if (!await _authService.IsAuthenticatedAsync())
                {
                    return "Error: No autenticado. Debe iniciar sesión para actualizar usuarios.";
                }

                var usuarioUpdate = new
                {
                    UsuarioId = oUsuarioDto.UsuarioId,
                    Nombre = oUsuarioDto.Nombre,
                    ApellidoPaterno = oUsuarioDto.ApellidoPaterno,
                    ApellidoMaterno = oUsuarioDto.ApellidoMaterno,
                    CorreoInstitucional = oUsuarioDto.CorreoInstitucional,
                    CI = oUsuarioDto.CI,
                    Rol = oUsuarioDto.Rol,
                    Estado = oUsuarioDto.Estado,
                    FechaRegistro = oUsuarioDto.FechaRegistro,
                    PasswordHash = oUsuarioDto.PasswordHash ?? ""
                };

                var response = await _authHttpClient.PutAsJsonAsync($"api/Usuarios/{id}", usuarioUpdate);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return "Error: No autorizado. Solo encargados y administradores pueden actualizar usuarios.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"Error: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        // DELETE: Cambiar estado (requiere token)
        public async Task<string> CambiarEstadoUsuario(int id)
        {
            try
            {
                // Verificar autenticación
                if (!await _authService.IsAuthenticatedAsync())
                {
                    return "Error: No autenticado. Debe iniciar sesión para cambiar el estado de usuarios.";
                }

                var response = await _authHttpClient.DeleteAsync($"api/Usuarios/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return "Error: No autorizado. Solo encargados y administradores pueden cambiar el estado de usuarios.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"Error: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }

    // Clase auxiliar para parsear respuestas con múltiples listas
    public class UsuarioResponse
    {
        public List<UsuarioDto> UsuariosEstudiantes { get; set; } = new();
        public List<UsuarioDto> UsuariosDocentes { get; set; } = new();
        public List<UsuarioDto> UsuariosEncargado { get; set; } = new();
        public List<UsuarioDto> UsuariosAdmin { get; set; } = new();
    }
}