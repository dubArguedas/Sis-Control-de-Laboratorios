using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCLAB_Entities
{
    public class MaquinaCLS
    {
        [Required]
        public int MaquinaId { get; set; }

        [Required]
        [MaxLength(50)]
        public string CodigoMaquina { get; set; } = string.Empty!;

        [Required]
        public int LaboratorioId { get; set; }
        public string? DescripcionHardware { get; set; }

        [MaxLength(20)]
        public string Estado { get; set; } = "disponible";
        public string? Qr { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}
