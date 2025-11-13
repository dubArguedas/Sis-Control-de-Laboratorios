using Blazored.LocalStorage;
using System.Net.Http.Headers;

namespace SCLAB_Client.Services
{
    public class AuthHttpClientHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;

        public AuthHttpClientHandler(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Obtener el token del localStorage
            var token = await _localStorage.GetItemAsync<string>("authToken");

            if (!string.IsNullOrEmpty(token))
            {
                // Agregar el token al header de autorización
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}