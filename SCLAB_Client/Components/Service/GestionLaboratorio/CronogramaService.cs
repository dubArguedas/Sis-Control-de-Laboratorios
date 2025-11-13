using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using SCLAB_Entities; // Asegúrate que CronogramaIntervalCLS y CronogramaResponse estén aquí

namespace SCLAB_Client.Services
{
    public class CronogramaService
    {
        private readonly HttpClient _httpClient;

        public CronogramaService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient");
        }
        public async Task<CronogramaResponse?> GetCronogramaResponse(int laboratorioId)
        {
            return await _httpClient.GetFromJsonAsync<CronogramaResponse>($"api/Cronograma/laboratorio/{laboratorioId}");
        }

        public async Task ActualizarMateria(int cronogramaId, string? materia)
        {
            var content = new StringContent(JsonSerializer.Serialize(materia), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/Cronograma/{cronogramaId}", content);
            response.EnsureSuccessStatusCode();
        }
    }

    public class CronogramaResponse
    {
        public int LaboratorioId { get; set; }
        public string CodigoLaboratorio { get; set; } = string.Empty!;
        public List<CronogramaIntervalCLS> Cronograma { get; set; } = new();
    }
}
