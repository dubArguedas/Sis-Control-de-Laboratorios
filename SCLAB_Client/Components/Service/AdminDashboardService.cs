using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SCLAB_Client.Models;

namespace SCLAB_Client.Components.Service
{
    public class AdminDashboardService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public AdminDashboardService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("AuthApiClient");
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };
        }

        // 1. Resumen de Laboratorios (Ultimate)
        public async Task<DashboardDataResponse?> ObtenerResumenLaboratorios()
        {
            return await _httpClient.GetFromJsonAsync<DashboardDataResponse>(
                "api/AdminDashboard/resumen-laboratorios", _jsonOptions);
        }

        // 2. Máquinas por Laboratorio
        public async Task<MaquinasLaboratorioResponse?> ObtenerMaquinasLaboratorio(int laboratorioId)
        {
            return await _httpClient.GetFromJsonAsync<MaquinasLaboratorioResponse>(
                $"api/AdminDashboard/laboratorio/{laboratorioId}", _jsonOptions);
        }
    }

    // --- DTOs Ultimate ---

    public class DashboardDataResponse
    {
        // Infraestructura
        [JsonPropertyName("totalLaboratorios")]
        public int TotalLaboratorios { get; set; }

        [JsonPropertyName("totalMaquinas")]
        public int TotalMaquinas { get; set; }

        [JsonPropertyName("totalMaquinasOperativas")]
        public int TotalMaquinasOperativas { get; set; }

        [JsonPropertyName("totalMaquinasDisponibles")]
        public int TotalMaquinasDisponibles { get; set; }

        [JsonPropertyName("totalMaquinasOcupadas")]
        public int TotalMaquinasOcupadas { get; set; }

        [JsonPropertyName("totalMaquinasMantenimiento")]
        public int TotalMaquinasMantenimiento { get; set; }

        [JsonPropertyName("totalMaquinasDescontinuadas")]
        public int TotalMaquinasDescontinuadas { get; set; }

        [JsonPropertyName("laboratorios")]
        public List<LaboratorioResumenDto> Laboratorios { get; set; } = new();

        // Usuarios
        [JsonPropertyName("totalUsuarios")]
        public int TotalUsuarios { get; set; }

        [JsonPropertyName("nuevosUsuariosMes")]
        public int NuevosUsuariosMes { get; set; }

        [JsonPropertyName("usuariosPorRol")]
        public UsuariosPorRolDto UsuariosPorRol { get; set; } = new();

        // Asistencia
        [JsonPropertyName("asistenciasActivas")]
        public int AsistenciasActivas { get; set; }

        [JsonPropertyName("asistenciasHoy")]
        public int AsistenciasHoy { get; set; }

        [JsonPropertyName("asistenciasSemana")]
        public int AsistenciasSemana { get; set; }

        [JsonPropertyName("chartAsistencias")]
        public List<ChartDataDto> ChartAsistencias { get; set; } = new();

        // Soporte
        [JsonPropertyName("alertasPendientes")]
        public int AlertasPendientes { get; set; }

        [JsonPropertyName("alertasResueltasHoy")]
        public int AlertasResueltasHoy { get; set; }

        [JsonPropertyName("alertasRecientes")]
        public List<AlertaRecienteDto> AlertasRecientes { get; set; } = new();

        // Uso (Nuevo)
        [JsonPropertyName("porcentajeUsoDiario")]
        public double PorcentajeUsoDiario { get; set; }

        [JsonPropertyName("porcentajeUsoSemanal")]
        public double PorcentajeUsoSemanal { get; set; }
    }

    public class UsuariosPorRolDto
    {
        [JsonPropertyName("admin")]
        public int Admin { get; set; }

        [JsonPropertyName("encargado")]
        public int Encargado { get; set; }

        [JsonPropertyName("docente")]
        public int Docente { get; set; }

        [JsonPropertyName("estudiante")]
        public int Estudiante { get; set; }
    }

    public class ChartDataDto
    {
        [JsonPropertyName("hora")]
        public string Hora { get; set; } = string.Empty;

        [JsonPropertyName("cantidad")]
        public int Cantidad { get; set; }
    }

    public class AlertaRecienteDto
    {
        [JsonPropertyName("alertaId")]
        public int AlertaId { get; set; }

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [JsonPropertyName("laboratorio")]
        public string Laboratorio { get; set; } = string.Empty;

        [JsonPropertyName("maquina")]
        public string Maquina { get; set; } = string.Empty;

        [JsonPropertyName("hace")]
        public string Hace { get; set; } = string.Empty;
    }

    public class LaboratorioResumenDto
    {
        [JsonPropertyName("laboratorioId")]
        public int LaboratorioId { get; set; }

        [JsonPropertyName("codigo")]
        public string Codigo { get; set; } = string.Empty;

        [JsonPropertyName("ubicacion")]
        public string Ubicacion { get; set; } = string.Empty;

        [JsonPropertyName("totalMaquinas")]
        public int TotalMaquinas { get; set; }

        [JsonPropertyName("alertasPendientes")]
        public int AlertasPendientes { get; set; }

        [JsonPropertyName("saludPorcentaje")]
        public double SaludPorcentaje { get; set; }
    }

    // --- DTOs Maquinas (Existentes) ---

    public class MaquinasLaboratorioResponse
    {
        [JsonPropertyName("laboratorioId")]
        public int LaboratorioId { get; set; }

        [JsonPropertyName("codigo")]
        public string Codigo { get; set; } = string.Empty;

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("maquinas")]
        public List<MaquinaResumenDto> Maquinas { get; set; } = new();
    }

    public class MaquinaResumenDto
    {
        [JsonPropertyName("maquinaId")]
        public int MaquinaId { get; set; }

        [JsonPropertyName("codigoMaquina")]
        public string CodigoMaquina { get; set; } = string.Empty;

        [JsonPropertyName("estado")]
        public string Estado { get; set; } = string.Empty;

        [JsonPropertyName("descripcionHardware")]
        public string DescripcionHardware { get; set; } = string.Empty;

        [JsonPropertyName("tieneQr")]
        public bool TieneQr { get; set; }

        [JsonPropertyName("alerta")]
        public AlertaResumenDto? AlertaActiva { get; set; }

        [JsonPropertyName("asignacion")]
        public AsignacionResumenDto? AsistenciaActiva { get; set; }
    }
        
    public class AlertaResumenDto
    {
        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; } = string.Empty;
    }

    public class AsignacionResumenDto
    {
        [JsonPropertyName("usuarioNombre")]
        public string UsuarioNombre { get; set; } = string.Empty;

        [JsonPropertyName("tiempoTranscurrido")]
        public int TiempoTranscurrido { get; set; }
    }
}