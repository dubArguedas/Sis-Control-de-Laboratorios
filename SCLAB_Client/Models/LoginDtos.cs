namespace SCLAB_Client.Models
{
    public class LoginDto
    {
        public string CorreoInstitucional { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public class UsuarioInfo
    {
        public int UsuarioId { get; set; }
        public string CorreoInstitucional { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }
}