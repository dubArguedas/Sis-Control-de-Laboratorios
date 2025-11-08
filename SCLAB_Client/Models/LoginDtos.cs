namespace SCLAB_Client.Models
{
    public class LoginDto
    {
        public string CorreoInstitucional { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // Cambia PasswordHash por Password
    }

    public class LoginResponse
    {
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public bool IsBlocked { get; set; }
        public TimeSpan TimeRemaining { get; set; }
        public int RemainingAttempts { get; set; }
        public string ErrorType { get; set; } = string.Empty;
    }

    public class UsuarioInfo
    {
        public int UsuarioId { get; set; }
        public string CorreoInstitucional { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }

    // AGREGA ESTA CLASE
    public class UsuarioBasico
    {
        public int UsuarioId { get; set; }
        public string CorreoInstitucional { get; set; } = string.Empty;
    }

}