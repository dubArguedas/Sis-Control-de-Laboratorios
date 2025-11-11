
using Microsoft.JSInterop;
using SCLAB_Client.Models;
using System.Net.Http.Json;

namespace SCLAB_Client.Components.Service
{
    public class UsuarioService
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime jSRuntime;

        public UsuarioService(HttpClient http, IJSRuntime _jsruntime)
        {
            _http = http;
            jSRuntime = _jsruntime;
        }

        // Obtener todos los usuarios
        public async Task<List<UsuarioDto>> GetUsuariosAsync()
        {
            try
            {
                var response = await _http.GetAsync("api/Usuarios");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<UsuarioDto>>() ?? new List<UsuarioDto>();
                }
                return new List<UsuarioDto>();
            }
            catch
            {
                return new List<UsuarioDto>();
            }
        }

        // Obtener usuario por ID
        public async Task<UsuarioDto?> GetUsuarioAsync(int id)
        {
            try
            {
                return await _http.GetFromJsonAsync<UsuarioDto>($"api/Usuarios/{id}");
            }
            catch
            {
                return null;
            }
        }

        // Crear usuario
        public async Task<(bool Success, string Message)> CreateUsuarioAsync(UsuarioCreateDto usuario)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/Usuarios", usuario);
                if (response.IsSuccessStatusCode)
                {
                    return (true, "Usuario creado exitosamente");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return (false, error);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        // Actualizar usuario
        public async Task<(bool Success, string Message)> UpdateUsuarioAsync(int id, UsuarioDto usuario)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"api/Usuarios/{id}", usuario);
                if (response.IsSuccessStatusCode)
                {
                    return (true, "Usuario actualizado exitosamente");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return (false, error);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        // Cambiar estado (eliminación lógica)
        public async Task<(bool Success, string Message)> ToggleUsuarioEstadoAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"api/Usuarios/{id}");
                if (response.IsSuccessStatusCode)
                {
                    return (true, "Estado del usuario cambiado exitosamente");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return (false, error);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }
    }

    public class LoginResponse
    {
        public string token { get; set; } = "";
        public UsuarioDto usuario { get; set; } = new UsuarioDto();
    }
}



















































//using Microsoft.JSInterop;
//namespace SCLAB_Client.Components.Service
//{
//    public class UsuarioService
//    {
//        private readonly HttpClient _http;
//        private readonly IJSRuntime jSRuntime;

//        public UsuarioService(HttpClient http, IJSRuntime _jsruntime)
//        {
//            _http = http;
//            jSRuntime = _jsruntime;
//        }
//        /* public async Task<(bool IsSuccess, string Token, string Rol, string Message)> LoginAsync(string correo, string password)
//        {
//            var response = await _http.PostAsJsonAsync("api/Usuarios/login", new { CorreoInstitucional = correo, Password = password });

//            if (!response.IsSuccessStatusCode)
//            {
//                var error = await response.Content.ReadAsStringAsync();
//                return (false, "", "", error);
//            }

//            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
//            return (true, result.token, result.usuario.Rol, "OK");
//        } */
//    }
//    public class LoginResponse
//    {
//        public string token { get; set; } = "";
//        public UsuarioDto usuario { get; set; } = new UsuarioDto();
//    }

//    public class UsuarioDto
//    {
//        public int UsuarioId { get; set; }
//        public string Rol { get; set; } = "";
//        public string Nombre { get; set; } = "";
//    }
//}
