using SCLAB_Client.Models;

namespace SCLAB_Client.Services
{
    public interface IAsistenciaService
    {
        Task<RegistroAsistenciaResponse?> RegistrarAsistencia(RegistroAsistenciaDto registro);
        Task<string> ActualizarObservacion(int asistenciaId, string observacion);
        Task<AsistenciaDto?> ObtenerAsistencia(int asistenciaId);
        Task<List<AsistenciaDto>> ObtenerAsistenciasPorUsuario(int usuarioId);
        Task<List<AsistenciaDto>> ObtenerAsistenciasActivasLaboratorio(int laboratorioId);
        Task<string> FinalizarAsistencia(int asistenciaId);
    }

    public class RegistroAsistenciaDto
    {
        public int UsuarioId { get; set; }
        public int MaquinaId { get; set; }
        public int LaboratorioId { get; set; }
    }

    public class RegistroAsistenciaResponse
    {
        public string Message { get; set; } = string.Empty;
        public int AsistenciaId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
        public int MaquinaId { get; set; }
        public string MaquinaCodigo { get; set; } = string.Empty;
        public int LaboratorioId { get; set; }
        public string LaboratorioCodigo { get; set; } = string.Empty;
        public int CronogramaId { get; set; }
        public string Materia { get; set; } = string.Empty;
        public DateTime HoraIngreso { get; set; }
        public string RegistroPor { get; set; } = string.Empty;
        public string TipoDispositivo { get; set; } = string.Empty;
    }

    public class AsistenciaDto
    {
        public int AsistenciaId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
        public UsuarioInfoDto? Usuario { get; set; }
        public int MaquinaId { get; set; }
        public MaquinaInfoDto? Maquina { get; set; }
        public int LaboratorioId { get; set; }
        public LaboratorioInfoDto? Laboratorio { get; set; }
        public int CronogramaId { get; set; }
        public CronogramaInfoDto? Cronograma { get; set; }
        public string RegistroPor { get; set; } = string.Empty;
        public DateTime HoraIngreso { get; set; }
        public DateTime? HoraSalida { get; set; }
        public string? DuracionUso { get; set; }
        public string RolRegistro { get; set; } = string.Empty;
        public string? Observacion { get; set; }
        public string TipoDispositivo { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
    }

    public class UsuarioInfoDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string? ApellidoMaterno { get; set; }
        public string CorreoInstitucional { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }

    public class MaquinaInfoDto
    {
        public string CodigoMaquina { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string? DescripcionHardware { get; set; }
    }

    public class LaboratorioInfoDto
    {
        public string CodigoLaboratorio { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }

    public class CronogramaInfoDto
    {
        public string Materia { get; set; } = string.Empty;
        public string DiaSemana { get; set; } = string.Empty;
        public string HoraInicio { get; set; } = string.Empty;
        public string HoraFin { get; set; } = string.Empty;
    }
}