using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCLAB_API.Models
{
    [Table("Laboratorio")]
    public class Laboratorio
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LaboratorioId { get; set; }

        [Required]
        [MaxLength(20)]
        public string CodigoLaboratorio { get; set; } = string.Empty!;

        [Required]
        [MaxLength(20)]
        public string Ubicacion { get; set; } = string.Empty!; // 'torre_maestra','torre_innovacion'

        [Required]
        public int Capacidad { get; set; }

        [MaxLength(20)]
        public string Estado { get; set; } = "disponible";

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Navegación
        public virtual  ICollection<Maquina>? Maquinas { get; set; }
        public virtual  ICollection<CronogramaInterval>? Cronogramas { get; set; }
        public virtual  ICollection<Asistencia>? Asistencias { get; set; }
        public virtual  ICollection<Alerta>? Alertas { get; set; }
    }
}
