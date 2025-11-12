namespace SCLAB_Client.Models
{
    /// <summary>
    /// DTO completo del usuario (lectura/actualización)
    /// </summary>
    public class UsuarioDto
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string? ApellidoMaterno { get; set; }
        public string CorreoInstitucional { get; set; } = string.Empty;
        public string CI { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string Estado { get; set; } = "activo";
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public string PasswordHash { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para crear usuarios (POST)
    /// No incluye UsuarioId, FechaRegistro ni Estado (se asignan en backend)
    /// </summary>
    public class UsuarioCreateDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string? ApellidoMaterno { get; set; }
        public string CorreoInstitucional { get; set; } = string.Empty;
        public string CI { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para actualizar usuarios (PUT)
    /// Solo campos que se pueden editar
    /// </summary>
    public class UsuarioUpdateDto
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string? ApellidoMaterno { get; set; }
        public string Estado { get; set; } = "activo";

        // Campos que NO se editan pero se necesitan para el PUT
        public string CorreoInstitucional { get; set; } = string.Empty;
        public string CI { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
    }

    /// <summary>
    /// Respuesta del login
    /// </summary>
    //public class LoginResponse
    //{
    //    public string token { get; set; } = string.Empty;
    //    public string message { get; set; } = string.Empty;
    //}

    /// <summary>
    /// Respuesta genérica de operaciones
    /// </summary>
    public class ApiResponse
    {
        public string message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Respuesta de error
    /// </summary>
    public class ErrorResponse
    {
        public string message { get; set; } = string.Empty;
        public string? detail { get; set; }
    }
}