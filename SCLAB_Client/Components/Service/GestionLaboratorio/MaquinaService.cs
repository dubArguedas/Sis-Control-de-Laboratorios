using SCLAB_Entities;
using System.Net.Http.Json;

namespace SCLAB_Client.Components.Service.GestionLaboratorio
{
    public class MaquinaService
    {
        private readonly HttpClient _http;

        public MaquinaService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<MaquinaListCLS>> ListarMaquinas()
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<MaquinaListCLS>>("api/Maquinas");

                return result?
                    .OrderBy(m => m.CodigoMaquina)
                    .ToList() ?? new List<MaquinaListCLS>();
            }
            catch (Exception)
            {
                return new List<MaquinaListCLS>();
            }
        }

        public async Task<MaquinaCLS?> ObtenerMaquina(int id)
        {
            try
            {
                return await _http.GetFromJsonAsync<MaquinaCLS>($"api/Maquinas/{id}");
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> CrearMaquina(MaquinaCLS maquina)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/Maquinas", maquina);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> ActualizarMaquina(int id, MaquinaCLS maquina)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"api/Maquinas/{id}", maquina);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> EliminarMaquina(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"api/Maquinas/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
