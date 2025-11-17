using SCLAB_Client.Components.Service.ServiciosApi;
using SCLAB_Client.Models;
using System.Net.Http.Json;

namespace SCLAB_Client.Services
{
    public interface IUsuarioService
    {
        Task<List<UsuarioDto>> ListarUsuarios();
        Task<UsuarioDto?> ObtenerUsuarioPorId(int usuarioId);
        Task<string> CrearUsuario(UsuarioCreateDto usuario);
        Task<string> ActualizarUsuario(UsuarioUpdateDto usuario);
        Task<string> CambiarEstadoUsuario(int usuarioId);
        Task<List<UsuarioDto>> ListarUsuariosPorRol(string rol);
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

        public async Task<List<UsuarioDto>> ListarUsuarios()
        {
            try
            {
                var token = _tokenState.GetToken();

                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("[UsuarioService] ⚠️ No hay token disponible para ListarUsuarios");
                    return new List<UsuarioDto>();
                }

                var response = await _httpClient.GetAsync("api/Usuarios");

                if (response.IsSuccessStatusCode)
                {
                    var usuarios = await response.Content.ReadFromJsonAsync<List<UsuarioDto>>();
                    return usuarios ?? new List<UsuarioDto>();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("[UsuarioService] ❌ No autorizado para listar usuarios");
                    return new List<UsuarioDto>();
                }
                else
                {
                    Console.WriteLine($"[UsuarioService] ❌ Error al listar usuarios: {response.StatusCode}");
                    return new List<UsuarioDto>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UsuarioService] ❌ Excepción al listar usuarios: {ex.Message}");
                return new List<UsuarioDto>();
            }
        }

        public async Task<List<UsuarioDto>> ListarUsuariosPorRol(string rol)
        {
            try
            {
                var token = _tokenState.GetToken();

                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine($"[UsuarioService] ⚠️ No hay token disponible para ListarUsuariosPorRol ({rol})");
                    return new List<UsuarioDto>();
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
                    return new List<UsuarioDto>();
                }
                else
                {
                    Console.WriteLine($"[UsuarioService] ❌ Error al listar usuarios por rol {rol}: {response.StatusCode}");
                    return new List<UsuarioDto>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UsuarioService] ❌ Excepción al listar usuarios por rol {rol}: {ex.Message}");
                return new List<UsuarioDto>();
            }
        }

        public async Task<UsuarioDto?> ObtenerUsuarioPorId(int usuarioId)
        {
            try
            {
                var token = _tokenState.GetToken();

                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("[UsuarioService] ⚠️ No hay token disponible para ObtenerUsuarioPorId");
                    return null;
                }

                var response = await _httpClient.GetAsync($"api/Usuarios/{usuarioId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UsuarioDto>();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("[UsuarioService] ❌ No autorizado para obtener usuario");
                    return null;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine("[UsuarioService] ❌ Usuario no encontrado");
                    return null;
                }
                else
                {
                    Console.WriteLine($"[UsuarioService] ❌ Error al obtener usuario: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UsuarioService] ❌ Excepción al obtener usuario: {ex.Message}");
                return null;
            }
        }

        public async Task<string> CrearUsuario(UsuarioCreateDto usuario)
        {
            try
            {
                var token = _tokenState.GetToken();

                if (string.IsNullOrEmpty(token))
                {
                    return "Error: No hay token de autenticación disponible";
                }

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
                var token = _tokenState.GetToken();

                if (string.IsNullOrEmpty(token))
                {
                    return "Error: No hay token de autenticación disponible";
                }

                // ENVIAR DIRECTAMENTE LO QUE RECIBIMOS DEL FORMULARIO
                // Si PasswordHash está vacío, la API mantendrá la contraseña actual
                // Si tiene valor, la API la hasheará y actualizará
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
                var token = _tokenState.GetToken();

                if (string.IsNullOrEmpty(token))
                {
                    return "Error: No hay token de autenticación disponible";
                }

                var response = await _httpClient.DeleteAsync($"api/Usuarios/{usuarioId}");

                if (response.IsSuccessStatusCode)
                {
                    return "Estado del usuario cambiado exitosamente";
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
    }
}