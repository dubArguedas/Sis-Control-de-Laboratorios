using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SCLAB_Client.Components.Service
{
    public class ReportesService
    {
        private readonly HttpClient _httpClient;

        public ReportesService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("AuthApiClient");
        }

        // 1. Distribución de Estados (Gráfico)
        // Endpoint: api/AdminDashboard/reportes/maquinas/distribucion-estados/{laboratorioId}
        public async Task<DistribucionEstadosResponse?> ObtenerDistribucionEstados(int laboratorioId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<DistribucionEstadosResponse>($"api/Reportes/reportes/maquinas/distribucion-estados/{laboratorioId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching distribution: {ex.Message}");
                return null;
            }
        }

        // 2. Asistencias por Materia (Docentes)
        // Endpoint: api/AdminDashboard/reportes/asistencias/{nombreMateria}
        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasPorMateria(string nombreMateria)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ReporteAsistenciasResponse>($"api/Reportes/reportes/asistencias/{nombreMateria}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching asistencias doc: {ex.Message}");
                return null;
            }
        }

        // 3. Asistencias por Materia (Estudiantes)
        // Endpoint: api/AdminDashboard/reportes/asistencias/busqueda/{nombreMateria}
        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasEstudiantesporMateria(string nombreMateria)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ReporteAsistenciasResponse>($"api/Reportes/reportes/asistencias/busqueda/{nombreMateria}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching asistencias est: {ex.Message}");
                return null;
            }
        }

        // 4. Asistencias por Horario
        // Endpoint: api/AdminDashboard/reportes/asistencias/horario/{dia}/{inicio}/{fin}
        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasporHorario(string diaSemana, TimeSpan inicio, TimeSpan fin)
        {
            try
            {
                // Formato hh:mm:ss requerido por la API para TimeSpans en URL
                string inicioStr = inicio.ToString(@"hh\:mm\:ss");
                string finStr = fin.ToString(@"hh\:mm\:ss");

                return await _httpClient.GetFromJsonAsync<ReporteAsistenciasResponse>($"api/Reportes/reportes/asistencias/horario/{diaSemana}/{inicioStr}/{finStr}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching asistencias horario: {ex.Message}");
                return null;
            }
        }

        // 5. Asistencias Generales (endpoints 12, 13, 14 del controlador)
        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasGeneral()
        {
            try { return await _httpClient.GetFromJsonAsync<ReporteAsistenciasResponse>("api/Reportes/reportes/asistencias/general"); }
            catch (Exception ex) { Console.WriteLine(ex.Message); return null; }
        }

        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasProgramada()
        {
            try { return await _httpClient.GetFromJsonAsync<ReporteAsistenciasResponse>("api/Reportes/reportes/asistencias/programada"); }
            catch (Exception ex) { Console.WriteLine(ex.Message); return null; }
        }

        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasUso_libre()
        {
            try { return await _httpClient.GetFromJsonAsync<ReporteAsistenciasResponse>("api/Reportes/reportes/asistencias/uso_libre"); }
            catch (Exception ex) { Console.WriteLine(ex.Message); return null; }
        }
    }

    // --- DTOs (Modelos de respuesta) ---
    // Estos deben estar aquí o en tu carpeta de Models para que el JSON sepa dónde guardarse

    public class DistribucionEstadosResponse
    {
        public int LaboratorioId { get; set; }
        public int TotalMaquinas { get; set; }
        public List<EstadoDistribucion> Distribucion { get; set; } = new();
    }

    public class EstadoDistribucion
    {
        public string Estado { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public double Porcentaje { get; set; }
    }

    public class ReporteAsistenciasResponse
    {
        public string? MateriaBuscada { get; set; }
        public string? DiaBuscado { get; set; }
        public int TotalAsistencias { get; set; }
        public string? Nota { get; set; }
        public List<AsistenciaReporteDto> Asistencias { get; set; } = new();
    }

    public class AsistenciaReporteDto
    {
        public int AsistenciaId { get; set; }

        // El controlador devuelve diferentes nombres de propiedades para el nombre del usuario
        // según el endpoint (DocenteNombre, EstudianteNombre, UsuarioNombre). Mapeamos todos.
        public string? DocenteNombre { get; set; }
        public string? EstudianteNombre { get; set; }
        public string? UsuarioNombre { get; set; }

        public string? CorreoInstitucional { get; set; }
        public string? Rol { get; set; }
        public string? LaboratorioCodigo { get; set; }
        public string? Materia { get; set; }
        public string? CronogramaHoraInicio { get; set; }
        public string? CronogramaHoraFin { get; set; }
        public TimeSpan HoraIngreso { get; set; }
        public TimeSpan? HoraSalida { get; set; }
        public int? DuracionUso { get; set; }
        public DateTime FechaRegistro { get; set; }

        // Propiedad auxiliar para mostrar el nombre en la tabla sin importar cuál venga lleno
        public string NombreMostrar => !string.IsNullOrEmpty(DocenteNombre) ? DocenteNombre :
                                       (!string.IsNullOrEmpty(EstudianteNombre) ? EstudianteNombre :
                                       UsuarioNombre ?? "Desconocido");
    }
}