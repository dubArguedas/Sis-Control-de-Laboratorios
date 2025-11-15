using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCLAB_Entities
{
    public class MaquinaListCLS
    {
        public int MaquinaId { get; set; }
        public string CodigoMaquina { get; set; } = string.Empty!;
        public string? DescripcionHardware { get; set; }
        public string Estado { get; set; } = string.Empty!;
        public int LaboratorioId { get; set; }
        public string LaboratorioCodigo { get; set; } = string.Empty!;
        public DateTime FechaRegistro { get; set; }
    }
}
