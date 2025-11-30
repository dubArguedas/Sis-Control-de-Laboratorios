using Org.BouncyCastle.Ocsp;
using SCLAB_Entities;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;


namespace SCLAB_Client.Components.Service.GestionLaboratorio
{
    public class RegistroAsistenciaDocenteDto
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
        public string materia { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
        public int asistenciaId { get; set; }
    }

    public class AsistenciaActivaDto
    {
        public int AsistenciaId { get; set; }
        public int UsuarioId { get; set; }   
        public string Materia { get; set; } = string.Empty; 
        public int MaquinaId { get; set; }    
    }
    public class RespuestaAsistenciasWrapper
    {
        public List<AsistenciaActivaDto> Asistencias { get; set; } = new();
    }

    public class AsistenciaDocenteGeneralDto
    {
        public int AsistenciaId { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
        public string CorreoInstitucional { get; set; } = string.Empty;
        public string LaboratorioCodigo { get; set; } = string.Empty;
        public string Materia { get; set; } = string.Empty;
        public DateTime HoraIngreso { get; set; }
        public DateTime? HoraSalida { get; set; }
        public TimeSpan? DuracionUso { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

    public class RespuestaAsistenciasGeneralDocenteDto
    {
        public int TotalAsistencias { get; set; }
        public List<AsistenciaDocenteGeneralDto> Asistencias { get; set; } = new();
    }

    public class DocenteService
    {
        private readonly HttpClient _http;

        public DocenteService(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient("AuthApiClient");
        }

        public async Task<UsuariosCLS> ObtenerDocenteId(int id)
        {
            try
            {
                if (id <= 0) return new UsuariosCLS();
                return await _http.GetFromJsonAsync<UsuariosCLS>($"api/Usuarios/{id}").ConfigureAwait(false) ?? new UsuariosCLS();
            }
            catch (Exception){return new UsuariosCLS();}
        }

        public async Task<ServiceResponse> RegistrarAsistenciaDocente(RegistroAsistenciaDocenteDto registroDto)
        {
            string jsonPayload = JsonSerializer.Serialize(registroDto, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(jsonPayload);

            var response = await _http.PostAsJsonAsync("api/AsistenciasDocente/registrar", registroDto).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                string message = "Registro exitoso.";
                RegistroExitosoDto data = new RegistroExitosoDto();

                try
                {
                    var successDto = await response.Content.ReadFromJsonAsync<RegistroExitosoDto>().ConfigureAwait(false);
                    if (successDto != null)
                    {
                        message = successDto.message;
                        data = successDto;
                    }
                }
                catch { /* Falla de deserialización, pero la respuesta HTTP fue 2xx */ }

                return new ServiceResponse { IsSuccess = true, Message = message, Data = data };
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

                return new ServiceResponse { IsSuccess = false, Message = message, Data = null };
            }
        }
        public async Task<ServiceResponse> ActualizarObservaciones(int asistenciaId, ActualizarObservacionDto dto)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"api/AsistenciasDocente/{asistenciaId}/observacion", dto).ConfigureAwait(false);

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
        public async Task<ServiceResponse> FinalizarAsistenciaDocente(int asistenciaId)
        {
            try
            {
                var response = await _http.PutAsync($"api/AsistenciasDocente/{asistenciaId}/finalizar", null).ConfigureAwait(false);

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
        public async Task<List<UsuariosCLS>> ObtenerEstudiantesPorMateria(string materia)
        {
            if (string.IsNullOrEmpty(materia)) return new List<UsuariosCLS>();

            try
            {
                var response = await _http.GetAsync($"api/AsistenciasDocente/materia/busqueda/{materia}").ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var root = await response.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);

                    if (root.TryGetProperty("asistencias", out JsonElement asistenciasArray))
                    {
                        var listaMapeada = new List<UsuariosCLS>();

                        foreach (var item in asistenciasArray.EnumerateArray())
                        {
                            var usuario = new UsuariosCLS
                            {
                                Nombre = item.TryGetProperty("estudianteNombre", out var nombre) ? nombre.GetString() : "",

                                CorreoInstitucional = item.TryGetProperty("correoInstitucional", out var correo) ? correo.GetString() : "",

                                FechaRegistro = item.TryGetProperty("fechaRegistro", out var fecha) ? fecha.GetDateTime() : DateTime.Now,

                                CI = ""
                            };
                            listaMapeada.Add(usuario);
                        }
                        return listaMapeada;
                    }
                }
                return new List<UsuariosCLS>();
            }
            catch
            {
                return new List<UsuariosCLS>();
            }
        }
        public async Task<List<AsistenciaActivaDto>> ObtenerAsistenciasDocentesActivas(int laboratorioId)
        {
            try
            {
                var response = await _http.GetAsync($"api/AsistenciasDocente/laboratorio/{laboratorioId}/activas").ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var wrapper = await response.Content.ReadFromJsonAsync<RespuestaAsistenciasWrapper>().ConfigureAwait(false);
                    
                    return wrapper?.Asistencias ?? new List<AsistenciaActivaDto>();
                }
                
                return new List<AsistenciaActivaDto>();
            }
            catch
            {
                return new List<AsistenciaActivaDto>();
            }
        }

        public async Task<ServiceResponse> ObtenerAsistenciasGeneralDocente()
        {
            try
            {
                var response = await _http.GetAsync("api/AsistenciasDocente/busqueda/general").ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<RespuestaAsistenciasGeneralDocenteDto>().ConfigureAwait(false);
                    return new ServiceResponse { IsSuccess = true, Data = result };
                }
                else
                {
                    return new ServiceResponse { IsSuccess = false, Message = $"Error: {response.StatusCode}" };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse { IsSuccess = false, Message = ex.Message };
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

