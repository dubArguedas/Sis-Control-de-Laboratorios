using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCLAB_Entities
{
    public class LogActividadCLS
    {
        [Required]
        public int LogId { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Accion { get; set; } = string.Empty!;
        public string? Detalle { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}
