using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCLAB_API.Models
{
    [Table("Alerta")]
    public class Alerta
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AlertaId { get; set; }

        [Required]
        public int MaquinaId { get; set; }

        public int? LaboratorioId { get; set; }

        [Required]
        public int CreadaPor { get; set; }

        [Required]
        [Column(TypeName = "NVARCHAR(MAX)")]
        public string Descripcion { get; set; } = string.Empty!;

        [MaxLength(20)]
        public string EstadoAlerta { get; set; } = "pendiente";

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? FechaResolucion { get; set; }

        public int? ResueltoPor { get; set; }

        [MaxLength(100)]
        public string? SolucionTipo { get; set; }

        [Column(TypeName = "NVARCHAR(MAX)")]
        public string? SolucionDescripcion { get; set; }

        // Navegación
        [ForeignKey("MaquinaId")]
        public virtual  Maquina? Maquina { get; set; }

        [ForeignKey("LaboratorioId")]
        public virtual Laboratorio? Laboratorio { get; set; }

        [ForeignKey("CreadaPor")]
        public virtual  Usuario? UsuarioCreador { get; set; }

        [ForeignKey("ResueltoPor")]
        public virtual Usuario? UsuarioResolutor { get; set; }
    }
}
