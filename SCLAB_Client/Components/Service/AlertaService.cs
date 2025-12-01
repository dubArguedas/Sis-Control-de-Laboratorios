using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static QRCoder.PayloadGenerator;
using static SCLAB_API.Controllers.AlertasController;

namespace SCLAB_Client.Components.Service
{
    public class CrearAlertaDto
    {
        public int MaquinaId { get; set; }
        public int UsuarioId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public bool CambiarEstadoMaquina { get; set; } = true;
    }

    public class ResolverAlertaDto
    {
        public int UsuarioId { get; set; }
        public string TipoSolucion { get; set; } = string.Empty;
        public string DescripcionSolucion { get; set; } = string.Empty;
        public string EstadoMaquinaDespues { get; set; } = "libre";
    }

    public class AlertaViewDto
    {
        public int AlertaId { get; set; }
        public MaquinaDto? Maquina { get; set; }
        public LaboratorioDto? Laboratorio { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string EstadoAlerta { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public string CreadaPor { get; set; } = string.Empty;
        public string? ResueltaPor { get; set; }
        public string? SolucionTipo { get; set; }
    }
    public class MaquinaDto { public int MaquinaId { get; set; } public string Codigo { get; set; } = string.Empty; }
    public class LaboratorioDto { public int LaboratorioId { get; set; } public string Codigo { get; set; } = string.Empty; }

    public class ErrorMessageDto
    {
        public string message { get; set; } = string.Empty;
        public string sugerencia { get; set; } = string.Empty;
    }

    public class ListaAlertasResponseDto
    {
        public int total { get; set; }
        public List<AlertaViewDto> alertas { get; set; } = new List<AlertaViewDto>();
    }

    public class ContadorPendientesDto
    {
        public int totalPendientes { get; set; }
    }

    public class AlertaService
    {
        private readonly HttpClient _http;

        public AlertaService(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient("AuthApiClient");
        }

        public async Task<ServiceResponse> CrearAlerta(CrearAlertaDto alertaDto)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/Alertas/alerta", alertaDto).ConfigureAwait(false);
                Console.WriteLine("[CREATE] alerta unu");
                if (response.IsSuccessStatusCode)
                {
                    return new ServiceResponse { IsSuccess = true, Message = "Alerta registrada correctamente." };
                }
                else
                {
                    string message = await LeerMensajeError(response);
                    return new ServiceResponse { IsSuccess = false, Message = message };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse { IsSuccess = false, Message = $"Error de conexión: {ex.Message}" };
            }
        }

        public async Task<ServiceResponse> ObtenerTodasAlertas()
        {
            try
            {
                var response = await _http.GetAsync("api/Alertas/alertas").ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = await response.Content.ReadFromJsonAsync<ListaAlertasResponseDto>(options).ConfigureAwait(false);

                    return new ServiceResponse
                    {
                        IsSuccess = true,
                        Message = "Alertas cargadas.",
                        Data = result?.alertas ?? new List<AlertaViewDto>()
                    };
                }
                else
                {
                    string message = await LeerMensajeError(response);
                    return new ServiceResponse { IsSuccess = false, Message = message, Data = new List<AlertaViewDto>() };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse { IsSuccess = false, Message = $"Error al cargar alertas: {ex.Message}", Data = new List<AlertaViewDto>() };
            }
        }

        public async Task<ServiceResponse> ResolverAlerta(int alertaId, ResolverAlertaDto resolverDto)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"api/Alertas/alerta/{alertaId}/resolver", resolverDto).ConfigureAwait(false);


                if (response.IsSuccessStatusCode)
                {
                    return new ServiceResponse { IsSuccess = true, Message = "Alerta resuelta exitosamente." };
                }
                else
                {
                    string message = await LeerMensajeError(response);
                    return new ServiceResponse { IsSuccess = false, Message = message };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse { IsSuccess = false, Message = $"Error al resolver: {ex.Message}" };
            }
        }

        // 4. Obtener Contador (Ya implementado previamente)
        public async Task<ServiceResponse> ObtenerContadorAlertasPendientes()
        {
            try
            {
                var response = await _http.GetAsync("api/Alertas/alertas/pendientes/contador").ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ContadorPendientesDto>().ConfigureAwait(false);
                    return new ServiceResponse { IsSuccess = true, Data = result?.totalPendientes ?? 0 };
                }
                return new ServiceResponse { IsSuccess = false, Data = 0 };
            }
            catch
            {
                return new ServiceResponse { IsSuccess = false, Data = 0 };
            }
        }
        public async Task<ServiceResponse> ObtenerAlertasPorEstado(string estado)
        {
            try
            {
                var response = await _http.GetAsync($"api/Alertas/alertas/estado/{estado}").ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = await response.Content.ReadFromJsonAsync<ListaAlertasResponseDto>(options).ConfigureAwait(false);

                    return new ServiceResponse
                    {
                        IsSuccess = true,
                        Message = "Alertas cargadas.",
                        Data = result?.alertas ?? new List<AlertaViewDto>()
                    };
                }
                else
                {
                    string message = await LeerMensajeError(response);
                    return new ServiceResponse { IsSuccess = false, Message = message, Data = new List<AlertaViewDto>() };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse { IsSuccess = false, Message = $"Error: {ex.Message}", Data = new List<AlertaViewDto>() };
            }
        }
        private async Task<string> LeerMensajeError(HttpResponseMessage response)
        {
            string message = $"Error servidor {(int)response.StatusCode}";
            try
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var errorDto = JsonSerializer.Deserialize<ErrorMessageDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (errorDto != null && !string.IsNullOrWhiteSpace(errorDto.message))
                    message = errorDto.message;
                else
                    message = body; // Si no es JSON, devolver texto crudo
            }
            catch { }
            return message;
        }
    }

    public class ServiceResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}