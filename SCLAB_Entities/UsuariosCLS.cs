using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCLAB_Entities
{
    public class UsuariosCLS
    {
        [Required]
        public int UsuarioId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty!;

        [Required]
        [MaxLength(100)]
        public string ApellidoPaterno { get; set; } = string.Empty!;

        [MaxLength(100)]
        public string? ApellidoMaterno { get; set; } = string.Empty!;

        [Required]
        [MaxLength(150)]
        public string CorreoInstitucional { get; set; } = string.Empty!;

        [Required]
        [MaxLength(20)]
        public string CI { get; set; } = string.Empty!;

        [Required]
        [MaxLength(20)]
        public string Rol { get; set; } = string.Empty!; // 'estudiante','docente','encargado','admin'

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty!;

        [MaxLength(10)]
        public string Estado { get; set; } = "activo"; // 'activo','inactivo'

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}
