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
        [Required(ErrorMessage = "El ID es obligatorio.")]
        public int LaboratorioId { get; set; }

        [Required(ErrorMessage = "El código es obligatorio.")]
        [MaxLength(20, ErrorMessage = "El código no debe exceder 20 caracteres.")]

        [RegularExpression(@"^(B40\d+|A30\d+)$", ErrorMessage = "El formato debe ser B40[Números] o A30[Números], e.g., B401, A305.")]
        public string CodigoLaboratorio { get; set; } = string.Empty!;

        [Required(ErrorMessage = "La ubicación es obligatoria.")]
        [MaxLength(20, ErrorMessage = "La ubicación no debe exceder 20 caracteres.")]
        public string Ubicacion { get; set; } = string.Empty!;

        public string Estado { get; set; } = "Libre";

        [Required(ErrorMessage = "La capacidad es obligatoria.")]
        [Range(-1, 100, ErrorMessage = "La capacidad debe ser entre 0 y 100.")]
        public int Capacidad { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}
