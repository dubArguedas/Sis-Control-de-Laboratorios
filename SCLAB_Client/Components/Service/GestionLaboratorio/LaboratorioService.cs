using Polly;
using SCLAB_Entities;
using System.Net.Http.Json;
using System.Text.Json;
using static System.Net.WebRequestMethods;
namespace SCLAB_Client.Services
{
    public class LaboratorioService
    {
        private readonly HttpClient _httpClient;
        private List<LaboratorioListCLS> lista;

        public LaboratorioService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("AuthApiClient");
            lista = new List<LaboratorioListCLS>();
        }

        public async Task<List<LaboratorioListCLS>> ListarLaboratorios()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<LaboratorioListCLS>>("api/Laboratorios");

                if (response == null)
                {
                    return new List<LaboratorioListCLS>();
                }
                else
                {
                    return response;
                }
            }
            catch
            {
                return new List<LaboratorioListCLS>();
            }
        }
        public async Task<string> CerrarLaboratorio (int id)
        {
            var response = await _httpClient.DeleteAsync("api/Laboratorios/" + id);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                return "Error: " + await response.Content.ReadAsStringAsync();
            }
        }
        public async Task<LaboratorioCLS> ObtenerLaboratorio(int id)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<LaboratorioCLS>("api/Laboratorios/" + id);

                if (response == null)
                {
                    return new LaboratorioCLS();
                }   
                else
                {
                    return response;
                }
            }
            catch
            {
                return new LaboratorioCLS();
            }
        }
        public async Task<bool> CrearLaboratorio(LaboratorioCLS oLaboratorioCLS)
        {
            var response = await _httpClient.PostAsJsonAsync("api/Laboratorios", oLaboratorioCLS);
            return response.IsSuccessStatusCode;
        }

        public async Task<(bool Exito, string Mensaje)> ActualizarEstadoLaboratorio(int id, string nuevoEstado)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/Laboratorios/{id}/estado", nuevoEstado);

                if (response.IsSuccessStatusCode)
                {
                    var contenido = await response.Content.ReadAsStringAsync();
                    return (true, contenido);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return (false, error);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error de conexión: {ex.Message}");
            }
        }

        public string ObtenerCodigoLaboratorio(int id)
        {
            var obj = lista.Where(p => p.LaboratorioId == id).FirstOrDefault();
            if (obj == null) return "";
            else return obj.CodigoLaboratorio;
        }

        public async Task<int> ObtenerIdLaboratorio(string codigo)
        {
            var laboratorios = await ListarLaboratorios();
            var obj = laboratorios.FirstOrDefault(p => p.CodigoLaboratorio == codigo);

            if (obj == null) return 0;
            else return obj.LaboratorioId;
        }
    }
}