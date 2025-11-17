using Microsoft.JSInterop;

namespace SCLAB_Client.Components.Service.ServiciosApi
{
    public interface ICookieService
    {
        Task SetCookieAsync(string name, string value, int expirationMinutes = 60);
        Task<string?> GetCookieAsync(string name);
        Task DeleteCookieAsync(string name);
    }

    public class CookieService : ICookieService
    {
        private readonly IJSRuntime _jsRuntime;

        public CookieService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task SetCookieAsync(string name, string value, int expirationMinutes = 60)
        {
            var expirationDate = DateTime.UtcNow.AddMinutes(expirationMinutes);
            var cookieString = $"{name}={value}; expires={expirationDate:R}; path=/; secure; samesite=strict";
            
            await _jsRuntime.InvokeVoidAsync("eval", $"document.cookie = '{cookieString}'");
        }

        public async Task<string?> GetCookieAsync(string name)
        {
            var cookies = await _jsRuntime.InvokeAsync<string>("eval", "document.cookie");
            
            if (string.IsNullOrEmpty(cookies))
                return null;

            var cookieDict = cookies.Split(';')
                .Select(c => c.Trim().Split('='))
                .Where(kvp => kvp.Length == 2)
                .ToDictionary(kvp => kvp[0], kvp => kvp[1]);

            return cookieDict.TryGetValue(name, out var value) ? value : null;
        }

        public async Task DeleteCookieAsync(string name)
        {
            var cookieString = $"{name}=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
            await _jsRuntime.InvokeVoidAsync("eval", $"document.cookie = '{cookieString}'");
        }
    }
}