using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.ComponentModel;
using SCLAB_API.Models;


namespace SCLAB_Client.Services
{
    public class UsuarioService
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;

        public UsuarioService(HttpClient httpClient, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
        }

        public async Task<Usuario?> GetUsuario(int id)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<SCLAB_API.Models.Usuario>($"api/usuarios/{id}");
                return response;
            }
            catch
            {
                return null;
            }
        }
        public async Task<Usuario?> GetUserByEmail(string _Email)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<Usuario>($"api/usuarios/email/{_Email}");
                if (response == null)
                {
                    return null;
                }
                return response;
            }
            catch
            {
                return null;
            }
        }

    }
}