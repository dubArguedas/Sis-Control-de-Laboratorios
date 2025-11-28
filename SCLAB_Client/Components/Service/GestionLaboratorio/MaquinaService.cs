using SCLAB_Entities;
using System.Net.Http.Json;

namespace SCLAB_Client.Components.Service.GestionLaboratorio
{
    public class MaquinaService
    {
        private readonly HttpClient _http;

        public MaquinaService(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient("AuthApiClient");
        }

        public async Task<List<MaquinaListCLS>> ListarMaquinas()
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<MaquinaListCLS>>("api/Maquinas");

                if(result == null)
                {
                    return new List<MaquinaListCLS>();
                }
                else
                {
                    return result;
                }
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


        public async Task<MaquinaCLS?> CrearMaquina(MaquinaCLS maquina)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/Maquinas", maquina);

                if (!response.IsSuccessStatusCode)
                    return null;

                var creada = await response.Content.ReadFromJsonAsync<MaquinaCLS>();
                return creada;
            }
            catch
            {
                return null;
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
        public async Task<bool> GenerarQr(int maquinaId)
        {
            try
            {
                var response = await _http.PutAsync($"api/Qr/generar/{maquinaId}", null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // MaquinaService.cs
        public async Task<int> ObtenerIdPorCodigo(string codigoMaquina) 
        {
            try
            {
                // 👈 CAMBIO 2: Usar await en lugar de .Result
                var response = await ListarMaquinas();
                var maquina = response.FirstOrDefault(m => m.CodigoMaquina == codigoMaquina);
                if (maquina != null)
                {
                    return maquina.MaquinaId;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

    }
}



