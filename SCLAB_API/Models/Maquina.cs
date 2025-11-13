using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCLAB_API.Models
{
    [Table("Maquina")]
    public class Maquina
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaquinaId { get; set; }

        [Required]
        [MaxLength(50)]
        public string CodigoMaquina { get; set; } = string.Empty!;

        [Required]
        public int LaboratorioId { get; set; }

        [Column(TypeName = "NVARCHAR(MAX)")]
        public string? DescripcionHardware { get; set; }

        [MaxLength(20)]
        public string Estado { get; set; } = "disponible";

        [Column(TypeName = "VARBINARY(MAX)")]
        public byte[]? Qr { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Navegación
        [ForeignKey("LaboratorioId")]
        public virtual Laboratorio? Laboratorio { get; set; }

        public virtual ICollection<Asistencia>? Asistencias { get; set; }
        public virtual ICollection<Alerta>? Alertas { get; set; }
    }
}
