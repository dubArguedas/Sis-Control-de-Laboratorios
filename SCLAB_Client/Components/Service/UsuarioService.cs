using SCLAB_Client.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace SCLAB_Client.Services
{
    public class UsuarioService
    {
        private readonly HttpClient _httpClient;

        public UsuarioService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient"); // Sin token para todas las operaciones
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

        // GET: Obtener usuario por ID (sin token)
        public async Task<UsuarioDto> ObtenerUsuario(int id)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<UsuarioDto>($"api/Usuarios/{id}");
                return response ?? new UsuarioDto();
            }
            catch
            {
                return new UsuarioDto();
            }
        }

        // POST: Crear usuario (sin token)
        public async Task<string> CrearUsuario(UsuarioCreateDto oUsuarioCreateDto)
        {
            try
            {
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

                var response = await _httpClient.PostAsJsonAsync("api/Usuarios", usuarioParaAPI);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return "Usuario creado correctamente";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    // Manejar errores específicos
                    if (errorContent.Contains("correo institucional ya existe"))
                        return "Error: El correo institucional ya está registrado";
                    else if (errorContent.Contains("CI ya existe"))
                        return "Error: El CI ya está registrado";
                    else
                        return $"Error: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        // PUT: Actualizar usuario (sin token)
        public async Task<string> ActualizarUsuario(int id, UsuarioDto oUsuarioDto)
        {
            try
            {
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

                var response = await _httpClient.PutAsJsonAsync($"api/Usuarios/{id}", usuarioUpdate);

                if (response.IsSuccessStatusCode)
                {
                    return "Usuario actualizado correctamente";
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

        // DELETE: Cambiar estado (sin token)
        public async Task<string> CambiarEstadoUsuario(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/Usuarios/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return "Estado del usuario cambiado correctamente";
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

        // Método adicional para obtener usuarios por rol específico
        public async Task<List<UsuarioDto>> ObtenerUsuariosPorRol(string rol)
        {
            try
            {
                var usuarios = await ListarUsuarios();
                return usuarios.Where(u => u.Rol == rol).ToList();
            }
            catch
            {
                return new List<UsuarioDto>();
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