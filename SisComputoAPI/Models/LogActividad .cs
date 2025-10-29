using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCLAB_API.Models
{
    [Table("LogActividad")]
    public class LogActividad
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LogId { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Accion { get; set; } = string.Empty!;

        [Column(TypeName = "NVARCHAR(MAX)")]
        public string? Detalle { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        // Navegación
        [ForeignKey("UsuarioId")]
        public virtual  Usuario? Usuario { get; set; }
    }
}
