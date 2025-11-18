using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCLAB_API.Models
{
    [Table("CronogramaInterval")]
    public class CronogramaInterval
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CronogramaId { get; set; }

        [Required]
        public int LaboratorioId { get; set; }

        [Required]
        [MaxLength(20)]
        public string DiaSemana { get; set; } = string.Empty!;

        [Required]
        public TimeSpan HoraInicio { get; set; }

        [Required]
        public TimeSpan HoraFin { get; set; }

        [MaxLength(150)]
        public string? Materia { get; set; }


        // Navegación
        [ForeignKey("LaboratorioId")]
        public virtual  Laboratorio? Laboratorio { get; set; }

        public virtual  ICollection<Asistencia>? Asistencias { get; set; }
    }
}
