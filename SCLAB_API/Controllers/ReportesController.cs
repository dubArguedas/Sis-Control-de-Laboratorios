using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCLAB_API.Data;

namespace SCLAB_API.Controllers
{
    /*
     * ═══════════════════════════════════════════════════════════════════════════════════════
     * CONTROLADOR DE REPORTES - ÍNDICE DE ENDPOINTS
     * ═══════════════════════════════════════════════════════════════════════════════════════
     * 
     * ───────────────────────────────────────────────────────────────────────────────────────
     * REPORTES DE MÁQUINAS
     * ───────────────────────────────────────────────────────────────────────────────────────
     * 
     * 1. GET /api/Reportes/reportes/maquinas/distribucion-estados/{laboratorioId}
     *    Método: ObtenerDistribucionEstados()
     *    Propósito: Distribución actual de estados de máquinas (para gráfico de torta)
     *    Parámetros ruta:
     *      - laboratorioId: ID del laboratorio
     *    Retorna: Por cada estado activo, cantidad y porcentaje
     * 
     * ───────────────────────────────────────────────────────────────────────────────────────
     * REPORTES DE ASISTENCIAS POR MATERIA
     * ───────────────────────────────────────────────────────────────────────────────────────
     * 
     * 2. GET /api/Reportes/reportes/asistencias/{nombreMateria}
     *    Método: ObtenerAsistenciasPorMateria()
     *    Propósito: Busca asistencias de DOCENTES por nombre de materia
     *    Parámetros ruta:
     *      - nombreMateria: Nombre o parte del nombre de la materia
     *    Retorna: Máximo 200 asistencias de docentes
     * 
     * 3. GET /api/Reportes/reportes/asistencias/busqueda/{nombreMateria}
     *    Método: ObtenerAsistenciasEstudiantesporMateria()
     *    Propósito: Busca asistencias de ESTUDIANTES por nombre de materia
     *    Parámetros ruta:
     *      - nombreMateria: Nombre o parte del nombre de la materia
     *    Retorna: Máximo 200 asistencias de estudiantes
     * 
     * ───────────────────────────────────────────────────────────────────────────────────────
     * REPORTES DE ASISTENCIAS POR HORARIO
     * ───────────────────────────────────────────────────────────────────────────────────────
     * 
     * 4. GET /api/Reportes/reportes/asistencias/horario/{diaSemana}/{horaInicioClase}/{horaFinClase}
     *    Método: ObtenerAsistenciasporHorario()
     *    Propósito: Busca asistencias de ESTUDIANTES por día y rango horario
     *    Parámetros ruta:
     *      - diaSemana: lunes, martes, miercoles, jueves, viernes, sabado, domingo
     *      - horaInicioClase: Formato TimeSpan (ej: 08:00:00)
     *      - horaFinClase: Formato TimeSpan (ej: 10:00:00)
     *    Retorna: Máximo 200 asistencias en el horario especificado
     * 
     * ───────────────────────────────────────────────────────────────────────────────────────
     * REPORTES GENERALES DE ASISTENCIAS
     * ───────────────────────────────────────────────────────────────────────────────────────
     * 
     * 5. GET /api/Reportes/reportes/asistencias/general
     *    Método: ObtenerAsistenciasGeneral()
     *    Propósito: Obtiene todas las asistencias del sistema (docentes y estudiantes)
     *    Retorna: Máximo 500 asistencias más recientes
     * 
     * 6. GET /api/Reportes/reportes/asistencias/programada
     *    Método: ObtenerAsistenciasProgramada()
     *    Propósito: Obtiene asistencias filtradas por tipo "programada"
     *    Retorna: Máximo 500 asistencias programadas más recientes
     * 
     * 7. GET /api/Reportes/reportes/asistencias/uso_libre
     *    Método: ObtenerAsistenciasuso_libre()
     *    Propósito: Obtiene asistencias filtradas por tipo "uso_libre"
     *    Retorna: Máximo 500 asistencias de uso libre más recientes
     */
    [Route("api/[controller]")]
    [ApiController]
    public class ReportesController : ControllerBase
    {
        private readonly SisComputoDbContext _context;

        public ReportesController(SisComputoDbContext context)
        {
            _context = context;
        }

        [HttpGet("reportes/maquinas/distribucion-estados/{laboratorioId}")]
        public async Task<IActionResult> ObtenerDistribucionEstados(int laboratorioId)
        {
            try
            {
                if (!await _context.Laboratorios.AnyAsync(l => l.LaboratorioId == laboratorioId))
                    return NotFound(new { message = "Laboratorio no encontrado" });

                var estados = await _context.Maquinas
                    .Where(m => m.LaboratorioId == laboratorioId
                        && m.Estado.ToLower() != "descontinuado"
                        && m.Estado.ToLower() != "cerrado")
                    .GroupBy(m => m.Estado.ToLower())
                    .Select(g => new
                    {
                        estado = g.Key,
                        cantidad = g.Count()
                    })
                    .ToListAsync();

                var total = estados.Sum(e => e.cantidad);

                var distribucion = estados.Select(e => new
                {
                    e.estado,
                    e.cantidad,
                    porcentaje = total > 0 ? Math.Round((e.cantidad / (double)total) * 100, 2) : 0
                });

                return Ok(new
                {
                    laboratorioId,
                    totalMaquinas = total,
                    distribucion
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener distribución", detail = ex.Message });
            }
        }

        [HttpGet("reportes/asistencias/{nombreMateria}")]
        public async Task<IActionResult> ObtenerAsistenciasPorMateria(string nombreMateria)
        {
            try
            {

                var asistencias = await _context.Asistencias
                    .Where(a => a.RolRegistro.ToLower() == "docente"
                        && a.Cronograma != null
                        && a.Cronograma.Materia != null
                        && a.Cronograma.Materia.ToLower().Contains(nombreMateria.ToLower()))
                    .OrderByDescending(a => a.FechaRegistro)
                    .Select(a => new
                    {
                        a.AsistenciaId,
                        DocenteNombre = a.Usuario!.Nombre + " " + a.Usuario.ApellidoPaterno,
                        a.Usuario.CorreoInstitucional,
                        LaboratorioCodigo = a.Laboratorio!.CodigoLaboratorio,
                        Materia = a.Cronograma!.Materia,
                        a.HoraIngreso,
                        a.HoraSalida,
                        a.DuracionUso,
                        a.Observacion,
                        a.FechaRegistro
                    })
                    .Take(200)
                    .ToListAsync();

                return Ok(new
                {
                    materiaBuscada = nombreMateria,
                    totalAsistencias = asistencias.Count,
                    asistencias
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        [HttpGet("reportes/asistencias/busqueda/{nombreMateria}")]
        public async Task<IActionResult> ObtenerAsistenciasEstudiantesporMateria(string nombreMateria)
        {
            try
            {

                var asistencias = await _context.Asistencias
                    .Where(a => a.RolRegistro.ToLower() == "estudiante"
                        && a.Cronograma != null
                        && a.Cronograma.Materia != null
                        && a.Cronograma.Materia.ToLower().Contains(nombreMateria.ToLower()))
                    .OrderByDescending(a => a.FechaRegistro)
                    .Select(a => new
                    {
                        a.AsistenciaId,
                        EstudianteNombre = a.Usuario!.Nombre + " " + a.Usuario.ApellidoPaterno,
                        a.Usuario.CorreoInstitucional,
                        LaboratorioCodigo = a.Laboratorio!.CodigoLaboratorio,
                        Materia = a.Cronograma!.Materia,
                        a.HoraIngreso,
                        a.HoraSalida,
                        a.DuracionUso,
                        a.Observacion,
                        a.FechaRegistro
                    })
                    .Take(200)
                    .ToListAsync();

                return Ok(new
                {
                    totalAsistencias = asistencias.Count,
                    asistencias
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        [HttpGet("reportes/asistencias/horario/{diaSemana}/{horaInicioClase}/{horaFinClase}")]
        public async Task<IActionResult> ObtenerAsistenciasporHorario(
            string diaSemana,
            TimeSpan horaInicioClase,
            TimeSpan horaFinClase)
        {
            try
            {

                if (horaInicioClase >= horaFinClase)
                    return BadRequest(new { message = "La hora de inicio debe ser menor a la hora de fin" });

                var diasValidos = new[] { "lunes", "martes", "miercoles", "jueves", "viernes", "sabado", "domingo" };
                if (!diasValidos.Contains(diaSemana.ToLower()))
                    return BadRequest(new { message = "Día de la semana inválido" });


                var asistencias = await _context.Asistencias
                    .Where(a => a.RolRegistro.ToLower() == "estudiante"
                        && a.Cronograma != null
                        && a.Cronograma.DiaSemana.ToLower() == diaSemana.ToLower()
                        && (a.Cronograma.HoraInicio <= horaFinClase && a.Cronograma.HoraFin >= horaInicioClase))
                    .OrderByDescending(a => a.FechaRegistro)
                    .Select(a => new
                    {
                        a.AsistenciaId,
                        EstudianteNombre = a.Usuario!.Nombre + " " + a.Usuario.ApellidoPaterno,
                        a.Usuario.CorreoInstitucional,
                        LaboratorioCodigo = a.Laboratorio!.CodigoLaboratorio,
                        Materia = a.Cronograma!.Materia,
                        CronogramaHoraInicio = a.Cronograma.HoraInicio.ToString(@"hh\:mm"),
                        CronogramaHoraFin = a.Cronograma.HoraFin.ToString(@"hh\:mm"),
                        a.HoraIngreso,
                        a.HoraSalida,
                        a.DuracionUso,
                        a.Observacion,
                        a.FechaRegistro
                    })
                    .Take(200)
                    .ToListAsync();

                return Ok(new
                {
                    diaBuscado = diaSemana,
                    horaInicioBuscada = horaInicioClase.ToString(@"hh\:mm"),
                    horaFinBuscada = horaFinClase.ToString(@"hh\:mm"),
                    totalAsistencias = asistencias.Count,
                    asistencias
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        [HttpGet("reportes/asistencias/general")]
        public async Task<IActionResult> ObtenerAsistenciasGeneral()
        {
            try
            {

                var asistencias = await _context.Asistencias
                    .OrderByDescending(a => a.FechaRegistro)
                    .Take(500)
                    .Select(a => new
                    {
                        a.AsistenciaId,
                        UsuarioNombre = a.Usuario!.Nombre + " " + a.Usuario.ApellidoPaterno,
                        a.Usuario.CorreoInstitucional,
                        a.Usuario.Rol,
                        LaboratorioCodigo = a.Laboratorio!.CodigoLaboratorio,
                        Materia = a.Cronograma != null ? a.Cronograma.Materia : null,
                        a.HoraIngreso,
                        a.HoraSalida,
                        a.DuracionUso,
                        a.Observacion,
                        a.FechaRegistro
                    })
                    .ToListAsync();

                return Ok(new
                {
                    totalAsistencias = asistencias.Count,
                    limite = 500,
                    nota = "Mostrando las últimas 500 asistencias",
                    asistencias
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }
        [HttpGet("reportes/asistencias/programada")]
        public async Task<IActionResult> ObtenerAsistenciasProgramada()
        {
            try
            {

                var asistencias = await _context.Asistencias
                    .Where(a => a.Tipo == "programada")
                    .OrderByDescending(a => a.FechaRegistro)
                    .Take(500)
                    .Select(a => new
                    {
                        a.AsistenciaId,
                        UsuarioNombre = a.Usuario!.Nombre + " " + a.Usuario.ApellidoPaterno,
                        a.Usuario.CorreoInstitucional,
                        a.Usuario.Rol,
                        LaboratorioCodigo = a.Laboratorio!.CodigoLaboratorio,
                        Materia = a.Cronograma != null ? a.Cronograma.Materia : null,
                        a.HoraIngreso,
                        a.HoraSalida,
                        a.DuracionUso,
                        a.Observacion,
                        a.FechaRegistro
                    })
                    .ToListAsync();

                return Ok(new
                {
                    totalAsistencias = asistencias.Count,
                    limite = 500,
                    nota = "Mostrando las últimas 500 asistencias",
                    asistencias
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }
        [HttpGet("reportes/asistencias/uso_libre")]
        public async Task<IActionResult> ObtenerAsistenciasuso_libre()
        {
            try
            {

                var asistencias = await _context.Asistencias
                    .OrderByDescending(a => a.FechaRegistro)
                    .Take(500)
                    .Where(a => a.Tipo == "uso_libre")
                    .Select(a => new
                    {
                        a.AsistenciaId,
                        UsuarioNombre = a.Usuario!.Nombre + " " + a.Usuario.ApellidoPaterno,
                        a.Usuario.CorreoInstitucional,
                        a.Usuario.Rol,
                        LaboratorioCodigo = a.Laboratorio!.CodigoLaboratorio,
                        Materia = a.Cronograma != null ? a.Cronograma.Materia : null,
                        a.HoraIngreso,
                        a.HoraSalida,
                        a.DuracionUso,
                        a.Observacion,
                        a.FechaRegistro
                    })
                    .ToListAsync();

                return Ok(new
                {
                    totalAsistencias = asistencias.Count,
                    limite = 500,
                    nota = "Mostrando las últimas 500 asistencias",
                    asistencias
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }
    }
}
