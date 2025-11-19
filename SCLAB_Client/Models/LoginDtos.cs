using System.ComponentModel.DataAnnotations;

namespace SCLAB_Client.Models
{
    public class LoginDto
    {
        [Required(ErrorMessage = "El correo institucional es requerido")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        public string CorreoInstitucional { get; set; } = string.Empty!; // Cambia a string.Empty!

        [Required(ErrorMessage = "La contraseña es requerida")]
        public string Password { get; set; } = string.Empty!; // Cambia a string.Empty! y quita MinLength
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? Message { get; set; }
        public string? ErrorType { get; set; }
        public bool IsBlocked { get; set; }
        public int RemainingAttempts { get; set; } = 3;
        public TimeSpan TimeRemaining { get; set; }
    }

    public class UsuarioInfo
    {
        public int UsuarioId { get; set; }
        public string CorreoInstitucional { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }

    public class UsuarioBasico
    {
        public int UsuarioId { get; set; }
        public string CorreoInstitucional { get; set; } = string.Empty;
    }
}