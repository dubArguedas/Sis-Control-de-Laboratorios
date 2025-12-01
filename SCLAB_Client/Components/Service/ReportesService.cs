using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SCLAB_Client.Components.Service
{
    public class ReportesService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public ReportesService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("AuthApiClient");
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };
        }

        // 1. Distribución de Estados (Gráfico)
        public async Task<DistribucionEstadosResponse?> ObtenerDistribucionEstados(int laboratorioId)
        {
            return await _httpClient.GetFromJsonAsync<DistribucionEstadosResponse>(
                $"api/Reportes/reportes/maquinas/distribucion-estados/{laboratorioId}", _jsonOptions);
        }

        // 2. Asistencias por Materia (Docentes)
        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasPorMateria(string nombreMateria)
        {
            return await _httpClient.GetFromJsonAsync<ReporteAsistenciasResponse>(
                $"api/Reportes/reportes/asistencias/{nombreMateria}", _jsonOptions);
        }

        // 3. Asistencias por Materia (Estudiantes)
        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasEstudiantesporMateria(string nombreMateria)
        {
            return await _httpClient.GetFromJsonAsync<ReporteAsistenciasResponse>(
                $"api/Reportes/reportes/asistencias/busqueda/{nombreMateria}", _jsonOptions);
        }

        // 4. Asistencias por Horario
        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasporHorario(string diaSemana, TimeSpan inicio, TimeSpan fin)
        {
            string inicioStr = inicio.ToString(@"hh\:mm\:ss");
            string finStr = fin.ToString(@"hh\:mm\:ss");

            return await _httpClient.GetFromJsonAsync<ReporteAsistenciasResponse>(
                $"api/Reportes/reportes/asistencias/horario/{diaSemana}/{inicioStr}/{finStr}", _jsonOptions);
        }

        // 5. Asistencias Generales
        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasGeneral()
        {
            return await _httpClient.GetFromJsonAsync<ReporteAsistenciasResponse>(
                "api/Reportes/reportes/asistencias/general", _jsonOptions);
        }

        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasProgramada()
        {
            return await _httpClient.GetFromJsonAsync<ReporteAsistenciasResponse>(
                "api/Reportes/reportes/asistencias/programada", _jsonOptions);
        }

        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasUso_libre()
        {
            return await _httpClient.GetFromJsonAsync<ReporteAsistenciasResponse>(
                "api/Reportes/reportes/asistencias/uso_libre", _jsonOptions);
        }
    }

    // --- DTOs ---

    public class DistribucionEstadosResponse
    {
        [JsonPropertyName("laboratorioId")]
        public int LaboratorioId { get; set; }

        [JsonPropertyName("totalMaquinas")]
        public int TotalMaquinas { get; set; }

        [JsonPropertyName("distribucion")]
        public List<EstadoDistribucion> Distribucion { get; set; } = new();
    }

    public class EstadoDistribucion
    {
        [JsonPropertyName("estado")]
        public string Estado { get; set; } = string.Empty;

        [JsonPropertyName("cantidad")]
        public int Cantidad { get; set; }

        [JsonPropertyName("porcentaje")]
        public double Porcentaje { get; set; }
    }

    public class ReporteAsistenciasResponse
    {
        [JsonPropertyName("materiaBuscada")]
        public string? MateriaBuscada { get; set; }

        [JsonPropertyName("diaBuscado")]
        public string? DiaBuscado { get; set; }

        [JsonPropertyName("totalAsistencias")]
        public int TotalAsistencias { get; set; }

        [JsonPropertyName("nota")]
        public string? Nota { get; set; }

        [JsonPropertyName("asistencias")]
        public List<AsistenciaReporteDto> Asistencias { get; set; } = new();
    }

    public class AsistenciaReporteDto
    {
        [JsonPropertyName("asistenciaId")]
        public int AsistenciaId { get; set; }

        [JsonPropertyName("docenteNombre")]
        public string? DocenteNombre { get; set; }

        [JsonPropertyName("estudianteNombre")]
        public string? EstudianteNombre { get; set; }

        [JsonPropertyName("usuarioNombre")]
        public string? UsuarioNombre { get; set; }

        [JsonPropertyName("correoInstitucional")]
        public string? CorreoInstitucional { get; set; }

        [JsonPropertyName("rol")]
        public string? Rol { get; set; }

        [JsonPropertyName("laboratorioCodigo")]
        public string? LaboratorioCodigo { get; set; }

        [JsonPropertyName("materia")]
        public string? Materia { get; set; }

        [JsonPropertyName("tipo")]
        public string? Tipo { get; set; } // "programada" o "uso_libre"

        [JsonPropertyName("registroPor")]
        public string? RegistroPor { get; set; } // "qr" o "administrador"

        [JsonPropertyName("cronogramaHoraInicio")]
        public string? CronogramaHoraInicio { get; set; }

        [JsonPropertyName("cronogramaHoraFin")]
        public string? CronogramaHoraFin { get; set; }

        [JsonPropertyName("horaIngreso")]
        public object? HoraIngresoRaw { get; set; }

        [JsonPropertyName("horaSalida")]
        public object? HoraSalidaRaw { get; set; }

        [JsonPropertyName("duracionUso")]
        public object? DuracionUso { get; set; }

        [JsonPropertyName("observacion")]
        public string? Observacion { get; set; }

        [JsonPropertyName("fechaRegistro")]
        public DateTime FechaRegistro { get; set; }

        // Propiedades calculadas
        public string NombreMostrar => !string.IsNullOrEmpty(DocenteNombre) ? DocenteNombre :
                                       (!string.IsNullOrEmpty(EstudianteNombre) ? EstudianteNombre :
                                       UsuarioNombre ?? "Desconocido");

        public string HoraIngresoStr => FormatTime(HoraIngresoRaw);
        public string HoraSalidaStr => FormatTime(HoraSalidaRaw);

        // Helper para formatear hora a HH:mm
        private string FormatTime(object? timeObj)
        {
            if (timeObj == null) return "-";
            
            // Intentar parsear si es string o TimeSpan
            string timeStr = timeObj.ToString() ?? "";
            
            if (TimeSpan.TryParse(timeStr, out TimeSpan ts))
            {
                return ts.ToString(@"hh\:mm");
            }
            
            if (DateTime.TryParse(timeStr, out DateTime dt))
            {
                return dt.ToString("HH:mm");
            }

            // Si ya viene como HH:mm:ss, cortar los segundos
            if (timeStr.Length >= 5 && timeStr.Contains(":"))
            {
                var parts = timeStr.Split(':');
                if (parts.Length >= 2)
                {
                    return $"{parts[0]}:{parts[1]}";
                }
            }

            return timeStr;
        }

        // Propiedades para UI y PDF
        public string TipoTexto => Tipo == "programada" ? "Programada" :
                                   Tipo == "uso_libre" ? "Uso Libre" :
                                   Tipo ?? "-";

        public string RegistroTexto => RegistroPor == "QR" ? "QR Estudiante" :
                                       RegistroPor == "administrador" ? "Administrador" :
                                       RegistroPor ?? "-";
    }
}