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

        // 1. Resumen de Laboratorios
        public async Task<ResumenLaboratoriosResponse?> ObtenerResumenLaboratorios()
        {
            return await _httpClient.GetFromJsonAsync<ResumenLaboratoriosResponse>(
                "api/AdminDashboard/resumen-laboratorios", _jsonOptions);
        }

        // 2. Máquinas por Laboratorio
        public async Task<MaquinasLaboratorioResponse?> ObtenerMaquinasLaboratorio(int laboratorioId)
        {
            return await _httpClient.GetFromJsonAsync<MaquinasLaboratorioResponse>(
                $"api/AdminDashboard/laboratorio/{laboratorioId}", _jsonOptions);
        }
    }

    // --- DTOs ---

    public class ResumenLaboratoriosResponse
    {
        [JsonPropertyName("totalLaboratorios")]
        public int TotalLaboratorios { get; set; }

        [JsonPropertyName("totalMaquinas")]
        public int TotalMaquinas { get; set; }

        [JsonPropertyName("laboratorios")]
        public List<LaboratorioResumenDto> Laboratorios { get; set; } = new();
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
    }

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