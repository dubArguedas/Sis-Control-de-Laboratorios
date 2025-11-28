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
        public string TipoDisp { get; set; } = "PC";
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

    public class AsistenciaDetalleDto
    {
        public int AsistenciaId { get; set; }
        public string? Materia { get; set; }
        public string? Observacion { get; set; }
        public string? MaquinaCodigo { get; set; }
        public string? LaboratorioCodigo { get; set; }
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

        // NUEVO: Método específico para registrar uso libre
        public async Task<ServiceResponse> RegistrarUsoLibre(RegistroAsistenciaDto registroDto)
        {
            string jsonPayload = JsonSerializer.Serialize(registroDto, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine("[RegistrarUsoLibre] Payload:");
            Console.WriteLine(jsonPayload);

            var response = await _http.PostAsJsonAsync("api/Asistencias/registrar/uso_libre", registroDto).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                string message = "Uso libre registrado exitosamente.";
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
                    }
                }
                catch { }

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

                    if (root.TryGetProperty("cronograma", out var cronograma) && cronograma.ValueKind != JsonValueKind.Null)
                    {
                        if (cronograma.TryGetProperty("materia", out var materia))
                        {
                            detalle.Materia = materia.GetString();
                        }
                    }

                    if (root.TryGetProperty("observacion", out var observacion) && observacion.ValueKind != JsonValueKind.Null)
                    {
                        detalle.Observacion = observacion.GetString();
                    }

                    // Mapear info de máquina y lab si es necesario para recuperar sesión
                    if (root.TryGetProperty("maquina", out var maquina) && maquina.ValueKind != JsonValueKind.Null)
                    {
                        if (maquina.TryGetProperty("codigoMaquina", out var codMaq)) detalle.MaquinaCodigo = codMaq.GetString();
                    }

                    if (root.TryGetProperty("laboratorio", out var laboratorio) && laboratorio.ValueKind != JsonValueKind.Null)
                    {
                        if (laboratorio.TryGetProperty("codigoLaboratorio", out var codLab)) detalle.LaboratorioCodigo = codLab.GetString();
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

        // NUEVO: Obtener asistencia activa del estudiante (sin hora de salida)
        public async Task<AsistenciaDetalleDto?> ObtenerAsistenciaActivaEstudiante(int usuarioId)
        {
            try
            {
                // Reutilizamos el endpoint que trae todas las asistencias del usuario
                // y filtramos en cliente la que no tenga hora de salida.
                // Idealmente, la API tendría un endpoint específico, pero esto funciona sin tocar API.
                var response = await _http.GetAsync($"api/Asistencias/usuario/{usuarioId}").ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var asistencias = await response.Content.ReadFromJsonAsync<List<AsistenciaResponseDto>>().ConfigureAwait(false);
                    
                    if (asistencias != null)
                    {
                        // Buscar la primera que NO tenga hora de salida
                        var activa = asistencias.FirstOrDefault(a => a.HoraSalida == null);
                        
                        if (activa != null)
                        {
                            return new AsistenciaDetalleDto
                            {
                                AsistenciaId = activa.AsistenciaId,
                                Materia = activa.Materia,
                                Observacion = activa.Observacion,
                                MaquinaCodigo = activa.MaquinaCodigo,
                                LaboratorioCodigo = activa.LaboratorioCodigo
                            };
                        }
                    }
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // DTO interno para mapear la respuesta de lista
        private class AsistenciaResponseDto
        {
            public int AsistenciaId { get; set; }
            public string? Materia { get; set; }
            public string? Observacion { get; set; }
            public DateTime? HoraSalida { get; set; }
            public string? MaquinaCodigo { get; set; }
            public string? LaboratorioCodigo { get; set; }
        }

        public async Task<List<UsuariosCLS>> BuscarEstudiantesPorNombre(string termino)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termino))
                    return new List<UsuariosCLS>();

                var response = await _http.GetAsync("api/Usuarios/estudiante").ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var estudiantes = await response.Content.ReadFromJsonAsync<List<UsuariosCLS>>().ConfigureAwait(false);

                    if (estudiantes != null)
                    {
                        return estudiantes
                            .Where(e => e.Nombre.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                                       e.ApellidoPaterno.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                                       e.CorreoInstitucional.Contains(termino, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }
                }

                return new List<UsuariosCLS>();
            }
            catch (Exception)
            {
                return new List<UsuariosCLS>();
            }
        }

        public async Task<List<AsistenciaDetalleCompleta>> ObtenerAsistenciasActivasPorLaboratorio(int laboratorioId)
        {
            try
            {
                var response = await _http.GetAsync($"api/Asistencias/laboratorio/{laboratorioId}/activas").ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var jsonDoc = JsonDocument.Parse(jsonResponse);
                    var root = jsonDoc.RootElement;

                    var lista = new List<AsistenciaDetalleCompleta>();

                    if (root.TryGetProperty("asistencias", out JsonElement asistenciasArray))
                    {
                        foreach (var item in asistenciasArray.EnumerateArray())
                        {
                            var detalle = new AsistenciaDetalleCompleta
                            {
                                AsistenciaId = item.GetProperty("asistenciaId").GetInt32(),
                                MaquinaId = item.GetProperty("maquinaId").GetInt32(),
                                UsuarioNombre = item.TryGetProperty("usuarioNombre", out var nombre) ? nombre.GetString() ?? "" : "",
                                Observacion = item.TryGetProperty("observacion", out var obs) && obs.ValueKind != JsonValueKind.Null ? obs.GetString() ?? "" : "",
                                HoraIngreso = item.TryGetProperty("horaIngreso", out var horaIngreso) ? horaIngreso.GetDateTime() : DateTime.Now
                            };
                            lista.Add(detalle);
                        }
                    }

                    return lista;
                }

                return new List<AsistenciaDetalleCompleta>();
            }
            catch (Exception)
            {
                return new List<AsistenciaDetalleCompleta>();
            }
        }

        public async Task<string> ObtenerUltimaObservacionMaquina(int maquinaId)
        {
            try
            {
                var response = await _http.GetAsync($"api/Asistencias/maquina/{maquinaId}/ultima-observacion").ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var jsonDoc = JsonDocument.Parse(jsonResponse);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("observacion", out var observacion))
                    {
                        return observacion.GetString() ?? "Sin observación registrada";
                    }
                }

                return "Sin observación registrada";
            }
            catch (Exception)
            {
                return "Error al obtener observación";
            }
        }

        public async Task<(bool permitido, string mensaje, string detalle)> VerificarHorarioUsoLibre()
        {
            try
            {
                var response = await _http.GetAsync("api/Asistencias/verificar-horario-uso-libre").ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var jsonDoc = JsonDocument.Parse(jsonResponse);
                    var root = jsonDoc.RootElement;

                    bool permitido = root.GetProperty("permitido").GetBoolean();
                    string mensaje = root.GetProperty("mensaje").GetString() ?? "";
                    string detalle = root.GetProperty("detalle").GetString() ?? "";

                    return (permitido, mensaje, detalle);
                }

                return (false, "Error de conexión", "No se pudo verificar el horario");
            }
            catch (Exception)
            {
                return (false, "Error", "Error al verificar horario");
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