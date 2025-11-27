using System.Net.Http.Json;
using SCLAB_Client.Models;

namespace SCLAB_Client.Components.Service
{
    public class AdminDashboardService
    {
        private readonly HttpClient _httpClient;

        public AdminDashboardService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("AuthApiClient");
        }

        public async Task<DistribucionEstadosResponse?> ObtenerDistribucionEstados(int laboratorioId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<DistribucionEstadosResponse>($"api/AdminDashboard/reportes/maquinas/distribucion-estados/{laboratorioId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching distribution: {ex.Message}");
                return null;
            }
        }

        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasPorMateria(string nombreMateria)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ReporteAsistenciasResponse>($"api/AdminDashboard/reportes/asistencias/{nombreMateria}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching asistencias por materia: {ex.Message}");
                return null;
            }
        }

        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasEstudiantesporMateria(string nombreMateria)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ReporteAsistenciasResponse>($"api/AdminDashboard/reportes/asistencias/busqueda/{nombreMateria}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching asistencias estudiantes por materia: {ex.Message}");
                return null;
            }
        }

        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasporHorario(string diaSemana, TimeSpan inicio, TimeSpan fin)
        {
            try
            {
                // Format TimeSpan as hh:mm:ss for URL if needed, or rely on default binding. 
                // API expects TimeSpan, usually default string format works or hh:mm
                // The controller uses [FromRoute] implicitly? No, it's part of the path.
                // Route: "reportes/asistencias/horario/{diaSemana}/{horaInicioClase}/{horaFinClase}"
                // TimeSpan in URL might be tricky. Let's format as hh:mm
                string inicioStr = inicio.ToString(@"hh\:mm");
                string finStr = fin.ToString(@"hh\:mm");
                
                return await _httpClient.GetFromJsonAsync<ReporteAsistenciasResponse>($"api/AdminDashboard/reportes/asistencias/horario/{diaSemana}/{inicioStr}/{finStr}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching asistencias por horario: {ex.Message}");
                return null;
            }
        }

        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasGeneral()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ReporteAsistenciasResponse>("api/AdminDashboard/reportes/asistencias/general");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching asistencias general: {ex.Message}");
                return null;
            }
        }

        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasProgramada()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ReporteAsistenciasResponse>("api/AdminDashboard/reportes/asistencias/programada");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching asistencias programada: {ex.Message}");
                return null;
            }
        }

        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasUso_libre()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ReporteAsistenciasResponse>("api/AdminDashboard/reportes/asistencias/uso_libre");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching asistencias uso libre: {ex.Message}");
                return null;
            }
        }
        public async Task<AlertaContadorResponse?> ObtenerContadorAlertasPendientes(int? laboratorioId = null)
        {
            try
            {
                var url = "api/AdminDashboard/alertas/pendientes/contador";
                if (laboratorioId.HasValue)
                {
                    url += $"?filtroLaboratorioId={laboratorioId.Value}";
                }
                return await _httpClient.GetFromJsonAsync<AlertaContadorResponse>(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching pending alerts count: {ex.Message}");
                return null;
            }
        }
        public async Task<ResumenLaboratoriosResponse?> ObtenerResumenLaboratorios()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ResumenLaboratoriosResponse>("api/AdminDashboard/resumen-laboratorios");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching lab summary: {ex.Message}");
                return null;
            }
        }

        public async Task<MaquinasLaboratorioResponse?> ObtenerMaquinasLaboratorio(int laboratorioId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<MaquinasLaboratorioResponse>($"api/AdminDashboard/laboratorio/{laboratorioId}/maquinas");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching machines: {ex.Message}");
                return null;
            }
        }
    }

    // DTOs
    public class ResumenLaboratoriosResponse
    {
        public int TotalLaboratorios { get; set; }
        public int TotalMaquinas { get; set; }
        public int TotalMaquinasEnFalla { get; set; }
        public double PorcentajeFalla { get; set; }
        public List<LaboratorioResumenDto> Laboratorios { get; set; } = new();
    }

    public class LaboratorioResumenDto
    {
        public int LaboratorioId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public int TotalMaquinas { get; set; }
        public EstadoMaquinasDto Estados { get; set; } = new();
        public int AlertasPendientes { get; set; }
        public double SaludPorcentaje { get; set; }
        public DateTime UltimaActualizacion { get; set; }
    }

    public class EstadoMaquinasDto
    {
        public int Disponibles { get; set; }
        public int Ocupadas { get; set; }
        public int Mantenimiento { get; set; }
    }

    public class MaquinasLaboratorioResponse
    {
        public int LaboratorioId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public int Total { get; set; }
        public List<MaquinaResumenDto> Maquinas { get; set; } = new();
    }

    public class MaquinaResumenDto
    {
        public int MaquinaId { get; set; }
        public string CodigoMaquina { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string DescripcionHardware { get; set; } = string.Empty;
        public bool TieneQr { get; set; }
        public TiempoDesdeRegistroDto TiempoDesdeRegistro { get; set; } = new();
        public AlertaResumenDto? Alerta { get; set; }
        public AsignacionResumenDto? Asignacion { get; set; }
    }

    public class TiempoDesdeRegistroDto
    {
        public int Dias { get; set; }
        public int Horas { get; set; }
    }

    public class AlertaResumenDto
    {
        public int AlertaId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
    }

    public class AsignacionResumenDto
    {
        public int UsuarioId { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
        public int TiempoTranscurrido { get; set; }
    }

    public class DistribucionEstadosResponse
    {
        public int LaboratorioId { get; set; }
        public int TotalMaquinas { get; set; }
        public List<EstadoDistribucion> Distribucion { get; set; } = new();
    }

    public class EstadoDistribucion
    {
        public string Estado { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public double Porcentaje { get; set; }
    }

    public class ReporteAsistenciasResponse
    {
        public string? MateriaBuscada { get; set; }
        public string? DiaBuscado { get; set; }
        public string? HoraInicioBuscada { get; set; }
        public string? HoraFinBuscada { get; set; }
        public int TotalAsistencias { get; set; }
        public string? Nota { get; set; }
        public List<AsistenciaReporteDto> Asistencias { get; set; } = new();
    }

    public class AsistenciaReporteDto
    {
        public int AsistenciaId { get; set; }
        public string? DocenteNombre { get; set; }
        public string? EstudianteNombre { get; set; }
        public string? UsuarioNombre { get; set; } // For general/programada/uso_libre
        public string? CorreoInstitucional { get; set; }
        public string? Rol { get; set; }
        public string? LaboratorioCodigo { get; set; }
        public string? Materia { get; set; }
        public string? CronogramaHoraInicio { get; set; }
        public string? CronogramaHoraFin { get; set; }
        public TimeSpan HoraIngreso { get; set; }
        public TimeSpan? HoraSalida { get; set; }
        public int? DuracionUso { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

    public class AlertaContadorResponse
    {
        public int TotalPendientes { get; set; }
    }
}
