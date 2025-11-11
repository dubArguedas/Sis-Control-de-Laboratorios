namespace SCLAB_Client.Models
{
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
}