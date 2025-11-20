using SCLAB_Client.Components.Service.ServiciosApi;
using SCLAB_Client.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace SCLAB_Client.Services
{
    public class AsistenciaService : IAsistenciaService
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenStateService _tokenState;

        public AsistenciaService(HttpClient httpClient, ITokenStateService tokenState)
        {
            _httpClient = httpClient;
            _tokenState = tokenState;

            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri("https://localhost:7241/");
            }
        }

        private void AgregarTokenHeader()
        {
            var token = _tokenState.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            }
        }

        public async Task<RegistroAsistenciaResponse?> RegistrarAsistencia(RegistroAsistenciaDto registro)
        {
            try
            {
                // No requiere token porque es [AllowAnonymous]
                var response = await _httpClient.PostAsJsonAsync("api/Asistencias/registrar", registro);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<RegistroAsistenciaResponse>();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[AsistenciaService] ❌ Error al registrar asistencia: {response.StatusCode} - {errorContent}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AsistenciaService] ❌ Excepción al registrar asistencia: {ex.Message}");
                return null;
            }
        }

        public async Task<string> ActualizarObservacion(int asistenciaId, string observacion)
        {
            try
            {
                AgregarTokenHeader();

                var dto = new { Observacion = observacion };
                var response = await _httpClient.PutAsJsonAsync($"api/Asistencias/{asistenciaId}/observacion", dto);

                if (response.IsSuccessStatusCode)
                {
                    return "Observación actualizada correctamente";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return "Error: No autorizado para actualizar observaciones";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return "Error: Asistencia no encontrada";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"Error: {response.StatusCode} - {errorContent}";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<AsistenciaDto?> ObtenerAsistencia(int asistenciaId)
        {
            try
            {
                AgregarTokenHeader();

                var response = await _httpClient.GetAsync($"api/Asistencias/{asistenciaId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<AsistenciaDto>();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[AsistenciaService] ❌ Error al obtener asistencia: {response.StatusCode} - {errorContent}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AsistenciaService] ❌ Excepción al obtener asistencia: {ex.Message}");
                return null;
            }
        }

        public async Task<List<AsistenciaDto>> ObtenerAsistenciasPorUsuario(int usuarioId)
        {
            try
            {
                AgregarTokenHeader();

                var response = await _httpClient.GetAsync($"api/Asistencias/usuario/{usuarioId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<AsistenciaDto>>() ?? new List<AsistenciaDto>();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[AsistenciaService] ❌ Error al obtener asistencias del usuario: {response.StatusCode} - {errorContent}");
                    return new List<AsistenciaDto>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AsistenciaService] ❌ Excepción al obtener asistencias del usuario: {ex.Message}");
                return new List<AsistenciaDto>();
            }
        }

        public async Task<List<AsistenciaDto>> ObtenerAsistenciasActivasLaboratorio(int laboratorioId)
        {
            try
            {
                AgregarTokenHeader();

                var response = await _httpClient.GetAsync($"api/Asistencias/laboratorio/{laboratorioId}/activas");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                    if (result.TryGetProperty("asistencias", out var asistenciasProp))
                    {
                        return JsonSerializer.Deserialize<List<AsistenciaDto>>(asistenciasProp.ToString()) ?? new List<AsistenciaDto>();
                    }
                }

                return new List<AsistenciaDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AsistenciaService] ❌ Excepción al obtener asistencias activas: {ex.Message}");
                return new List<AsistenciaDto>();
            }
        }

        public async Task<string> FinalizarAsistencia(int asistenciaId)
        {
            try
            {
                AgregarTokenHeader();

                var response = await _httpClient.PutAsync($"api/Asistencias/{asistenciaId}/finalizar", null);

                if (response.IsSuccessStatusCode)
                {
                    return "Asistencia finalizada correctamente";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return "Error: No autorizado para finalizar asistencias";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return "Error: Asistencia no encontrada";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"Error: {response.StatusCode} - {errorContent}";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}