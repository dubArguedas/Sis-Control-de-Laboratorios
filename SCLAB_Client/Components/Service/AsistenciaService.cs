using SCLAB_Entities;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SCLAB_Client.Services
{
    public class RegistroAsistenciaDto
    {
        public int UsuarioId { get; set; }
        public int MaquinaId { get; set; }
        public int LaboratorioId { get; set; }
    }

    public class ActualizarObservacionDto
    {
        public string Observacion { get; set; } = string.Empty;
    }

    public class ErrorMessageDto
    {
        public string message { get; set; } = string.Empty;
        public string sugerencia { get; set; } = string.Empty;
    }

    public class RegistroExitosoDto
    {
        public string message { get; set; } = string.Empty;
        public int asistenciaId { get; set; }
    }

    // Clase simple para obtener solo los datos que necesitamos
    public class AsistenciaDetalleDto
    {
        public int AsistenciaId { get; set; }
        public string? Materia { get; set; }
        public string? Observacion { get; set; }
    }

    public class AsistenciaService
    {
        private readonly HttpClient _http;

        public AsistenciaService(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient("AuthApiClient");
        }

        public async Task<UsuariosCLS> ObtenerEstudianteId(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return new UsuariosCLS();
                }
                return await _http.GetFromJsonAsync<UsuariosCLS>($"api/Usuarios/{id}").ConfigureAwait(false) ?? new UsuariosCLS();
            }
            catch (Exception)
            {
                return new UsuariosCLS();
            }
        }

        public async Task<ServiceResponse> RegistrarAsistencia(RegistroAsistenciaDto registroDto)
        {
            string jsonPayload = JsonSerializer.Serialize(registroDto, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(jsonPayload);

            var response = await _http.PostAsJsonAsync("api/Asistencias/registrar", registroDto).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                string message = "Registro exitoso.";
                int asistenciaId = 0;

                try
                {
                    var successDto = await response.Content.ReadFromJsonAsync<RegistroExitosoDto>().ConfigureAwait(false);
                    if (successDto != null)
                    {
                        message = successDto.message;
                        asistenciaId = successDto.asistenciaId;
                    }
                }
                catch { }

                return new ServiceResponse { IsSuccess = true, Message = message, Data = asistenciaId };
            }
            else
            {
                string message = $"Error de servidor. Código: {(int)response.StatusCode}";

                try
                {
                    var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (response.StatusCode == HttpStatusCode.BadRequest && !string.IsNullOrWhiteSpace(responseBody))
                    {
                        var errorDto = JsonSerializer.Deserialize<ErrorMessageDto>(responseBody);
                        if (errorDto != null && !string.IsNullOrWhiteSpace(errorDto.message))
                        {
                            message = $"{errorDto.message}. {errorDto.sugerencia}";
                        }
                        else
                        {
                            message = $"Fallo de validación (400): {responseBody}";
                        }
                    }
                    else
                    {
                        message = $"Error {(int)response.StatusCode}: {responseBody}";
                    }
                }
                catch (Exception)
                {
                    message = $"Error de servidor no reconocido. Código: {(int)response.StatusCode}";
                }

                return new ServiceResponse { IsSuccess = false, Message = message, Data = 0 };
            }
        }

        public async Task<ServiceResponse> ActualizarObservaciones(int asistenciaId, ActualizarObservacionDto dto)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"api/Asistencias/{asistenciaId}/observacion", dto).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadFromJsonAsync<RegistroExitosoDto>().ConfigureAwait(false);
                    string message = body?.message ?? "Observación guardada y estado de máquina actualizado.";

                    return new ServiceResponse { IsSuccess = true, Message = message };
                }
                else
                {
                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    string message = $"Error de servidor. Código: {(int)response.StatusCode}";

                    try
                    {
                        var error = JsonSerializer.Deserialize<ErrorMessageDto>(body);
                        if (error != null && !string.IsNullOrWhiteSpace(error.message))
                        {
                            message = error.message;
                        }
                        else
                        {
                            message = $"Error de procesamiento: {body}";
                        }
                    }
                    catch { }

                    return new ServiceResponse { IsSuccess = false, Message = message };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse { IsSuccess = false, Message = $"Error de conexión: {ex.Message}" };
            }
        }

        public async Task<ServiceResponse> FinalizarAsistencia(int asistenciaId)
        {
            try
            {
                var response = await _http.PutAsync($"api/Asistencias/{asistenciaId}/finalizar", null).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string message = "Asistencia finalizada exitosamente.";
                    try
                    {
                        var successDto = await response.Content.ReadFromJsonAsync<RegistroExitosoDto>().ConfigureAwait(false);
                        if (successDto != null && !string.IsNullOrWhiteSpace(successDto.message))
                        {
                            message = successDto.message;
                        }
                    }
                    catch { }
                    return new ServiceResponse { IsSuccess = true, Message = message };
                }
                else
                {
                    string message = $"Error de servidor. Código: {(int)response.StatusCode}";

                    try
                    {
                        var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        var errorDto = JsonSerializer.Deserialize<ErrorMessageDto>(responseBody);
                        if (errorDto != null && !string.IsNullOrWhiteSpace(errorDto.message))
                        {
                            message = errorDto.message;
                        }
                        else
                        {
                            message = $"Error {(int)response.StatusCode}: {responseBody}";
                        }
                    }
                    catch { }

                    return new ServiceResponse { IsSuccess = false, Message = message };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse { IsSuccess = false, Message = $"Error de Conexión: {ex.Message}" };
            }
        }

        public async Task<AsistenciaDetalleDto?> ObtenerAsistenciaDetalle(int asistenciaId)
        {
            try
            {
                var response = await _http.GetAsync($"api/Asistencias/{asistenciaId}").ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var jsonDoc = JsonDocument.Parse(jsonResponse);
                    var root = jsonDoc.RootElement;

                    var detalle = new AsistenciaDetalleDto
                    {
                        AsistenciaId = root.GetProperty("asistenciaId").GetInt32()
                    };

                    // Extraer materia del cronograma
                    if (root.TryGetProperty("cronograma", out var cronograma) && cronograma.ValueKind != JsonValueKind.Null)
                    {
                        if (cronograma.TryGetProperty("materia", out var materia))
                        {
                            detalle.Materia = materia.GetString();
                        }
                    }

                    // Extraer observación si existe
                    if (root.TryGetProperty("observacion", out var observacion) && observacion.ValueKind != JsonValueKind.Null)
                    {
                        detalle.Observacion = observacion.GetString();
                    }

                    return detalle;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public class ServiceResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}