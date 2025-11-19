using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCLAB_Entities
{
    public class CronogramaIntervalCLS
    {
        [Required]
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

        [MaxLength(50)]
        public string? Materia { get; set; }
        public string? Observacion { get; set; }
    }
}
