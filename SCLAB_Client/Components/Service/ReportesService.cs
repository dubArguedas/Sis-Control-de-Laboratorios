using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Text;
using System.Net; // Necesario para HttpStatusCode

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

        #region 1. Reportes de Máquinas (Distribución)

        public async Task<DistribucionEstadosResponse?> ObtenerDistribucionEstados(int laboratorioId)
        {
            if (laboratorioId <= 0) throw new ArgumentException("Seleccione un laboratorio válido.");

            return await SendRequestAsync<DistribucionEstadosResponse>(
                $"api/Reportes/reportes/maquinas/distribucion-estados/{laboratorioId}");
        }

        #endregion

        #region 2. Reportes por Materia (Unificado con Fecha y Rol y AHORA MÁQUINAS)

        /// <summary>
        /// Busca asistencias por materia, permitiendo filtrar por rol, fecha, y ubicación/máquina.
        /// </summary>
        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasPorMateria(
            string nombreMateria,
            string rol = "",
            DateTime? fecha = null,
            // Parámetros opcionales nuevos
            string? maquina = null,
            int? laboratorioId = null,
            string? ubicacion = null)
            {
                if (string.IsNullOrWhiteSpace(nombreMateria))
                    throw new ArgumentException("Debe ingresar o seleccionar una materia.");

                var queryParams = new List<string>();

                // Parámetro obligatorio
                queryParams.Add($"nombreMateria={Uri.EscapeDataString(nombreMateria)}");

                // Parámetros opcionales
                if (!string.IsNullOrEmpty(rol)) queryParams.Add($"rol={Uri.EscapeDataString(rol)}");
                if (fecha.HasValue) queryParams.Add($"fecha={fecha.Value.ToString("yyyy-MM-dd")}");

                // Filtros de Máquina (Cascada)
                if (!string.IsNullOrEmpty(maquina)) queryParams.Add($"maquina={Uri.EscapeDataString(maquina)}");
                if (laboratorioId.HasValue && laboratorioId.Value > 0) queryParams.Add($"laboratorioId={laboratorioId.Value}");
                if (!string.IsNullOrEmpty(ubicacion)) queryParams.Add($"ubicacion={Uri.EscapeDataString(ubicacion)}");

                string url = "api/Reportes/reportes/asistencias/por-materia?" + string.Join("&", queryParams);

                return await SendRequestAsync<ReporteAsistenciasResponse>(url);
            }

        // Método de compatibilidad (redirige al nuevo)
        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasEstudiantesporMateria(string nombreMateria)
            => await ObtenerAsistenciasPorMateria(nombreMateria, "estudiante");

        #endregion

        #region 3. Reportes por Horario (Con Filtros en Cascada)

        /// <summary>
        /// Busca por horario con filtros opcionales de Máquina, Laboratorio y Ubicación (Torre).
        /// </summary>
        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasporHorario(
            string diaSemana,
            TimeSpan inicio,
            TimeSpan fin,
            string? maquina = null,
            int? laboratorioId = null,
            string? ubicacion = null)
        {
            // Validaciones básicas
            if (inicio >= fin) throw new ArgumentException("La hora de inicio debe ser menor a la hora de fin.");
            if (string.IsNullOrWhiteSpace(diaSemana)) throw new ArgumentException("El día es requerido.");

            string diaNormalizado = RemoveDiacritics(diaSemana).ToLower();
            string inicioStr = inicio.ToString(@"hh\:mm\:ss");
            string finStr = fin.ToString(@"hh\:mm\:ss");

            // URL Base con parámetros de ruta obligatorios
            string url = $"api/Reportes/reportes/asistencias/horario/{diaNormalizado}/{inicioStr}/{finStr}";

            // Construcción de Query String con los filtros opcionales
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(maquina))
                queryParams.Add($"maquina={Uri.EscapeDataString(maquina)}");

            if (laboratorioId.HasValue && laboratorioId.Value > 0)
                queryParams.Add($"laboratorioId={laboratorioId.Value}");

            if (!string.IsNullOrEmpty(ubicacion))
                queryParams.Add($"ubicacion={Uri.EscapeDataString(ubicacion)}");

            // Si hay filtros opcionales, los añadimos a la URL
            if (queryParams.Any())
            {
                url += "?" + string.Join("&", queryParams);
            }

            return await SendRequestAsync<ReporteAsistenciasResponse>(url);
        }

        #endregion

        #region 4. Reportes Generales

        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasGeneral() =>
            await SendRequestAsync<ReporteAsistenciasResponse>("api/Reportes/reportes/asistencias/general");

        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasProgramada() =>
            await SendRequestAsync<ReporteAsistenciasResponse>("api/Reportes/reportes/asistencias/programada");

        public async Task<ReporteAsistenciasResponse?> ObtenerAsistenciasUso_libre() =>
            await SendRequestAsync<ReporteAsistenciasResponse>("api/Reportes/reportes/asistencias/uso_libre");

        #endregion

        #region Core: Manejo de Peticiones y Errores

        /// <summary>
        /// Método centralizado para peticiones GET. Captura errores del backend y los devuelve limpios.
        /// </summary>
        private async Task<T?> SendRequestAsync<T>(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.NoContent) return default;
                    return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                }

                // Manejo de errores (400, 404, 500)
                var errorContent = await response.Content.ReadAsStringAsync();
                string mensajeUsuario = $"Error del servidor ({response.StatusCode})";

                try
                {
                    // Intentamos extraer el mensaje "human readable" que envía el backend
                    using var doc = JsonDocument.Parse(errorContent);
                    if (doc.RootElement.TryGetProperty("message", out var msg))
                        mensajeUsuario = msg.GetString() ?? mensajeUsuario;
                    else if (doc.RootElement.TryGetProperty("title", out var title))
                        mensajeUsuario = title.GetString() ?? mensajeUsuario;
                }
                catch { /* Si falla el parseo JSON, nos quedamos con el mensaje genérico */ }

                // Lanzamos excepción simple para que el UI la muestre en el NotificationService
                throw new ApplicationException(mensajeUsuario);
            }
            catch (HttpRequestException)
            {
                throw new ApplicationException("No se pudo conectar al servidor. Verifique su conexión.");
            }
        }

        private string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalizedString)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        #endregion
    }

    #region DTOs (Modelos de Datos)

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

        [JsonPropertyName("docenteNombre")]
        public string? DocenteNombre { get; set; }

        [JsonPropertyName("estudianteNombre")]
        public string? EstudianteNombre { get; set; }

        [JsonPropertyName("usuarioNombre")]
        public string? UsuarioNombre { get; set; }

        public string? CorreoInstitucional { get; set; }
        public string? Rol { get; set; }
        public string? LaboratorioCodigo { get; set; }
        public string? Materia { get; set; }
        public string? Tipo { get; set; }
        public string? RegistroPor { get; set; }

        // Horarios del cronograma
        public string? CronogramaHoraInicio { get; set; }
        public string? CronogramaHoraFin { get; set; }

        // Máquina
        [JsonPropertyName("maquinaCodigo")]
        public string? MaquinaCodigo { get; set; }

        // Horas reales (ingreso/salida)
        [JsonPropertyName("horaIngreso")]
        public object? HoraIngresoRaw { get; set; }

        [JsonPropertyName("horaSalida")]
        public object? HoraSalidaRaw { get; set; }

        public object? DuracionUso { get; set; }
        public string? Observacion { get; set; }
        public DateTime FechaRegistro { get; set; }

        // PROPIEDADES COMPUTADAS (Para la vista)
        public string NombreMostrar => !string.IsNullOrEmpty(DocenteNombre) ? DocenteNombre :
                                       (!string.IsNullOrEmpty(EstudianteNombre) ? EstudianteNombre :
                                       UsuarioNombre ?? "Desconocido");

        public string HoraIngresoStr => FormatTime(HoraIngresoRaw);
        public string HoraSalidaStr => FormatTime(HoraSalidaRaw);

        private string FormatTime(object? timeObj)
        {
            if (timeObj == null) return "-";
            string timeStr = timeObj.ToString() ?? "";

            if (TimeSpan.TryParse(timeStr, out TimeSpan ts)) return ts.ToString(@"hh\:mm");
            if (DateTime.TryParse(timeStr, out DateTime dt)) return dt.ToString("HH:mm");

            if (timeStr.Length >= 5 && timeStr.Contains(":"))
            {
                var parts = timeStr.Split(':');
                if (parts.Length >= 2) return $"{parts[0]}:{parts[1]}";
            }
            return timeStr;
        }
    }

    #endregion
}