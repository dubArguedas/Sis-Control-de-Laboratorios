using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCLAB_Entities
{
    public class LaboratorioCLS
    {
        [Required]
        public int LaboratorioId { get; set; }

        [Required]
        [MaxLength(20)]
        public string CodigoLaboratorio { get; set; } = string.Empty!;

        [Required]
        [MaxLength(20)]
        public string Ubicacion { get; set; } = string.Empty!; // 'torre_maestra','torre_innovacion'

        [Required]
        public int Capacidad { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}
