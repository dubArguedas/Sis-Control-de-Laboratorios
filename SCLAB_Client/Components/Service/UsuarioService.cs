
using Microsoft.JSInterop;

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
        /* public async Task<(bool IsSuccess, string Token, string Rol, string Message)> LoginAsync(string correo, string password)
        {
            var response = await _http.PostAsJsonAsync("api/Usuarios/login", new { CorreoInstitucional = correo, Password = password });

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return (false, "", "", error);
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            return (true, result.token, result.usuario.Rol, "OK");
        } */
    }
    public class LoginResponse
    {
        public string token { get; set; } = "";
        public UsuarioDto usuario { get; set; } = new UsuarioDto();
    }

    public class UsuarioDto
    {
        public int UsuarioId { get; set; }
        public string Rol { get; set; } = "";
        public string Nombre { get; set; } = "";
    }
}
