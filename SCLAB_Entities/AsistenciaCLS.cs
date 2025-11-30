using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCLAB_Entities
{
    public class AsistenciaCLS
    {
        [Required]
        public int AsistenciaId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Tipo { get; set; } = string.Empty!;

        [Required]
        public int UsuarioId { get; set; }

        [Required]
        public int MaquinaId { get; set; }

        [Required]
        public int LaboratorioId { get; set; }

        public int? CronogramaId { get; set; }

        [Required]
        [MaxLength(20)]
        public string RegistroPor { get; set; } = string.Empty!;

        public DateTime HoraIngreso { get; set; } = DateTime.Now;

        public DateTime? HoraSalida { get; set; }

        [Required]
        [MaxLength(20)]
        public string RolRegistro { get; set; } = string.Empty!;
        public string? Observacion { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
    //una clase nomas xd

    public class AsistenciaDetalleCompleta
    {
        public int AsistenciaId { get; set; }
        public int MaquinaId { get; set; }
        public string UsuarioNombre { get; set; } = "";
        public string Observacion { get; set; } = "";
    }
}
