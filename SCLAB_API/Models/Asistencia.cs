using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCLAB_API.Models
{
    [Table("Asistencia")]
    public class Asistencia
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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

        [NotMapped]
        public TimeSpan? DuracionUso => HoraSalida.HasValue ? HoraSalida - HoraIngreso : null;


        [Required]
        [MaxLength(20)]
        public string RolRegistro { get; set; } = string.Empty!;

        [Column(TypeName = "NVARCHAR(MAX)")]
        public string? Observacion { get; set; }

        [MaxLength(20)]
        public string TipoDispositivo { get; set; } = "PC";


        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Navegación
        [ForeignKey("UsuarioId")]
        public virtual  Usuario? Usuario { get; set; }

        [ForeignKey("MaquinaId")]
        public virtual  Maquina? Maquina { get; set; }

        [ForeignKey("LaboratorioId")]
        public virtual  Laboratorio? Laboratorio { get; set; }

        [ForeignKey("CronogramaId")]
        public virtual  CronogramaInterval? Cronograma { get; set; }
    }
}
