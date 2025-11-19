namespace SCLAB_Client.Components.Service.ServiciosApi
{
    public interface ITokenStateService
    {
        string? GetToken();
        void SetToken(string token);
        void ClearToken();
    }

    public class TokenStateService : ITokenStateService
    {
        private string? _token;

        public string? GetToken() => _token;

        public void SetToken(string token)
        {
            _token = token;
        }

        public void ClearToken()
        {
            _token = null;
        }
    }
}