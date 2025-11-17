using System.Net.Http.Headers;

namespace SCLAB_Client.Components.Service.ServiciosApi
{
    public class AuthHttpClientHandler : DelegatingHandler
    {
        private readonly ITokenStateService _tokenState;

        public AuthHttpClientHandler(ITokenStateService tokenState)
        {
            _tokenState = tokenState;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Obtener el token del estado en memoria (servidor)
            var token = _tokenState.GetToken();

            // 🔍 LOG PARA DEPURACIÓN (eliminar en producción)
            Console.WriteLine($"[AuthHttpClientHandler] URL: {request.RequestUri}");
            Console.WriteLine($"[AuthHttpClientHandler] Token presente: {!string.IsNullOrEmpty(token)}");
            if (!string.IsNullOrEmpty(token))
            {
                Console.WriteLine($"[AuthHttpClientHandler] Token (primeros 20 chars): {token.Substring(0, Math.Min(20, token.Length))}...");
            }

            if (!string.IsNullOrEmpty(token))
            {
                // Agregar el token al header de autorización
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                Console.WriteLine($"[AuthHttpClientHandler] Header Authorization agregado");
            }
            else
            {
                Console.WriteLine($"[AuthHttpClientHandler] ⚠️ NO hay token disponible");
            }

            var response = await base.SendAsync(request, cancellationToken);

            // 🔍 LOG de respuesta
            Console.WriteLine($"[AuthHttpClientHandler] Status Code: {response.StatusCode}");
            
            return response;
        }
    }
}