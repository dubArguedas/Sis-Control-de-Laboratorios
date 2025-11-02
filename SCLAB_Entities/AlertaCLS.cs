using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCLAB_Entities
{
    public class AlertaCLS
    {
        [Required]
        public int AlertaId { get; set; }

        [Required]
        public int MaquinaId { get; set; }

        public int? LaboratorioId { get; set; }

        [Required]
        public int CreadaPor { get; set; }

        [Required]
        public string Descripcion { get; set; } = string.Empty!;

        [MaxLength(20)]
        public string EstadoAlerta { get; set; } = "pendiente";

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? FechaResolucion { get; set; }

        public int? ResueltoPor { get; set; }

        [MaxLength(20)]
        public string? SolucionTipo { get; set; }
        public string? SolucionDescripcion { get; set; }
    }
}
