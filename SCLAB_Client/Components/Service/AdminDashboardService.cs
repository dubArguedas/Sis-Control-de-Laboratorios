using System.Net.Http.Json;
using System.Text.Json.Serialization;
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

        // 1. Resumen de Laboratorios
        // Endpoint: api/AdminDashboard/resumen-laboratorios
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

        // 2. Máquinas por Laboratorio
        // Endpoint: api/AdminDashboard/laboratorio/{laboratorioId}
        public async Task<MaquinasLaboratorioResponse?> ObtenerMaquinasLaboratorio(int laboratorioId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<MaquinasLaboratorioResponse>($"api/AdminDashboard/laboratorio/{laboratorioId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching machines: {ex.Message}");
                return null;
            }
        }
    }

    // --- DTOs del Dashboard Operativo ---

    public class ResumenLaboratoriosResponse
    {
        public int TotalLaboratorios { get; set; }
        public int TotalMaquinas { get; set; }
        public List<LaboratorioResumenDto> Laboratorios { get; set; } = new();
    }

    public class LaboratorioResumenDto
    {
        public int LaboratorioId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public int TotalMaquinas { get; set; }
        // Se puede mapear 'estados' y 'saludPorcentaje' si se necesitan aquí
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

        // Mapeo exacto de los objetos anidados del controlador (Endpoint 2)
        [JsonPropertyName("alerta")]
        public AlertaResumenDto? AlertaActiva { get; set; }

        [JsonPropertyName("asignacion")]
        public AsignacionResumenDto? AsistenciaActiva { get; set; }
    }
        
    public class AlertaResumenDto
    {
        public string Descripcion { get; set; } = string.Empty;
    }

    public class AsignacionResumenDto
    {
        public string UsuarioNombre { get; set; } = string.Empty;
        public int TiempoTranscurrido { get; set; }
    }
}