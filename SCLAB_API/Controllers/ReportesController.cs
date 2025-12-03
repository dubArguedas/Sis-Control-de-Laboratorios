using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCLAB_API.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;

namespace SCLAB_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportesController : ControllerBase
    {
        private readonly SisComputoDbContext _context;

        public ReportesController(SisComputoDbContext context)
        {
            _context = context;
        }

        // =============================================================
        // 1. DISTRIBUCIÓN (GRÁFICO Y DETALLES)
        // =============================================================
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
                    .Select(g => new { estado = g.Key, cantidad = g.Count() })
                    .ToListAsync();

                var total = estados.Sum(e => e.cantidad);
                var distribucion = estados.Select(e => new { e.estado, e.cantidad, porcentaje = total > 0 ? Math.Round((e.cantidad / (double)total) * 100, 2) : 0 });

                return Ok(new { laboratorioId, totalMaquinas = total, distribucion });
            }
            catch (Exception ex) { return StatusCode(500, new { message = "Error interno", detail = ex.Message }); }
        }

        [HttpGet("laboratorio/{laboratorioId}/maquinas-detalle")]
        public async Task<IActionResult> ObtenerMaquinasLaboratorio(int laboratorioId)
        {
            try
            {
                var lab = await _context.Laboratorios.FirstOrDefaultAsync(l => l.LaboratorioId == laboratorioId);
                if (lab == null) return NotFound(new { message = "Laboratorio no encontrado" });

                var maquinas = await _context.Maquinas
                    .Include(m => m.Asistencias).ThenInclude(a => a.Usuario)
                    .Where(m => m.LaboratorioId == laboratorioId && m.Estado.ToLower() != "eliminado")
                    .OrderBy(m => m.CodigoMaquina)
                    .ToListAsync();

                var resultado = maquinas.Select(m => {
                    var asistenciaActiva = m.Asistencias?
                        .Where(a => a.HoraSalida == null && a.FechaRegistro.Date == DateTime.Today)
                        .OrderByDescending(a => a.HoraIngreso)
                        .FirstOrDefault();

                    string nombreUsuarioActivo = "Usuario";
                    if (asistenciaActiva != null && asistenciaActiva.Usuario != null)
                    {
                        nombreUsuarioActivo = $"{asistenciaActiva.Usuario.Nombre} {asistenciaActiva.Usuario.ApellidoPaterno}".Trim();
                    }

                    return new
                    {
                        m.MaquinaId,
                        m.CodigoMaquina,
                        m.Estado,
                        DescripcionHardware = m.DescripcionHardware ?? "Sin descripción",
                        TieneQr = m.Qr != null && m.Qr.Length > 0,
                        AsistenciaActiva = asistenciaActiva != null ? new
                        {
                            UsuarioNombre = nombreUsuarioActivo,
                            TiempoTranscurrido = (int)(DateTime.Now - asistenciaActiva.HoraIngreso).TotalMinutes
                        } : null,
                        AlertaActiva = (object)null
                    };
                });
                return Ok(new { lab.CodigoLaboratorio, Maquinas = resultado });
            }
            catch (Exception ex) { return StatusCode(500, new { message = "Error interno", detail = ex.Message }); }
        }

        // =============================================================
        // 2. ASISTENCIAS POR HORARIO (MODIFICADO PARA SOPORTAR "GENERAL")
        // =============================================================
        [HttpGet("reportes/asistencias/horario/{diaSemana}/{horaInicioClase}/{horaFinClase}")]
        public async Task<IActionResult> ObtenerAsistenciasporHorario(
            string diaSemana, TimeSpan horaInicioClase, TimeSpan horaFinClase,
            [FromQuery] string? maquina = null,
            [FromQuery] int? laboratorioId = null,
            [FromQuery] string? ubicacion = null)
        {
            try
            {
                if (horaInicioClase >= horaFinClase) return BadRequest(new { message = "Horas inválidas" });

                // 1. Consulta Base
                var query = _context.Asistencias
                    .Include(a => a.Usuario).Include(a => a.Laboratorio).Include(a => a.Cronograma).Include(a => a.Maquina)
                    .Where(a => a.Cronograma != null);

                // 2. LÓGICA DE DÍA GENERAL:
                // Si diaSemana NO es "general", aplicamos el filtro de día exacto.
                // Si ES "general", no filtramos por día (trae lunes, martes, etc.), solo por hora.
                if (diaSemana.ToLower() != "general")
                {
                    query = query.Where(a => a.Cronograma.DiaSemana.ToLower() == diaSemana.ToLower());
                }

                // 3. Filtro de Hora (Aplica siempre a cualquier día seleccionado)
                // Busca clases que se solapen o estén contenidas en el rango
                query = query.Where(a => a.Cronograma.HoraInicio <= horaFinClase && a.Cronograma.HoraFin >= horaInicioClase);

                // 4. Filtros Opcionales
                if (!string.IsNullOrEmpty(maquina)) query = query.Where(a => a.Maquina != null && a.Maquina.CodigoMaquina.Contains(maquina));
                else if (laboratorioId.HasValue && laboratorioId.Value > 0) query = query.Where(a => a.LaboratorioId == laboratorioId.Value);
                else if (!string.IsNullOrEmpty(ubicacion)) query = query.Where(a => a.Laboratorio != null && a.Laboratorio.Ubicacion == ubicacion);

                var resultado = await query
                    .OrderByDescending(a => a.FechaRegistro)
                    .Select(a => new
                    {
                        a.AsistenciaId,
                        DocenteNombre = a.Usuario != null ? (a.Usuario.Nombre + " " + a.Usuario.ApellidoPaterno).Trim() : "",
                        EstudianteNombre = a.Usuario != null ? (a.Usuario.Nombre + " " + a.Usuario.ApellidoPaterno).Trim() : "",
                        UsuarioNombre = a.Usuario != null ? (a.Usuario.Nombre + " " + a.Usuario.ApellidoPaterno).Trim() : "Usuario Eliminado",

                        LaboratorioCodigo = a.Laboratorio.CodigoLaboratorio,
                        Materia = a.Cronograma.Materia,
                        // Devolvemos también el día para saber cuál fue si se buscó "General"
                        Dia = a.Cronograma.DiaSemana,
                        CronogramaHoraInicio = a.Cronograma.HoraInicio.ToString(@"hh\:mm"),
                        CronogramaHoraFin = a.Cronograma.HoraFin.ToString(@"hh\:mm"),
                        MaquinaCodigo = a.Maquina != null ? a.Maquina.CodigoMaquina : "-",
                        a.HoraIngreso,
                        a.HoraSalida,
                        a.Observacion,
                        a.FechaRegistro,
                        a.Tipo,
                        Rol = a.Usuario != null ? a.Usuario.Rol : "N/A"
                    })
                    .Take(500) // Limitamos para rendimiento si es "General"
                    .ToListAsync();

                return Ok(new { diaBuscado = diaSemana, totalAsistencias = resultado.Count, asistencias = resultado });
            }
            catch (Exception ex) { return StatusCode(500, new { message = "Error interno", detail = ex.Message }); }
        }

        // =============================================================
        // 3. REPORTES POR MATERIA
        // =============================================================
        [HttpGet("reportes/asistencias/por-materia")]
        public async Task<IActionResult> ObtenerAsistenciasPorMateriaUnificado(
            [FromQuery] string nombreMateria,
            [FromQuery] string rol,
            [FromQuery] DateTime? fecha = null,
            [FromQuery] string? maquina = null,
            [FromQuery] int? laboratorioId = null,
            [FromQuery] string? ubicacion = null)
        {
            try
            {
                if (string.IsNullOrEmpty(nombreMateria)) return BadRequest(new { message = "La materia es requerida" });

                var query = _context.Asistencias
                    .Include(a => a.Usuario).Include(a => a.Laboratorio).Include(a => a.Cronograma).Include(a => a.Maquina)
                    .Where(a => a.Cronograma != null && a.Cronograma.Materia.ToLower().Contains(nombreMateria.ToLower()));

                if (!string.IsNullOrEmpty(rol)) query = query.Where(a => a.RolRegistro.ToLower() == rol.ToLower());
                if (fecha.HasValue) query = query.Where(a => a.FechaRegistro.Date == fecha.Value.Date);

                if (!string.IsNullOrEmpty(maquina))
                {
                    query = query.Where(a => a.Maquina != null && a.Maquina.CodigoMaquina.Contains(maquina));
                }
                else if (laboratorioId.HasValue && laboratorioId.Value > 0)
                {
                    query = query.Where(a => a.LaboratorioId == laboratorioId.Value);
                }
                else if (!string.IsNullOrEmpty(ubicacion))
                {
                    query = query.Where(a => a.Laboratorio != null && a.Laboratorio.Ubicacion == ubicacion);
                }

                var data = await query
                    .OrderByDescending(a => a.FechaRegistro)
                    .Select(a => new {
                        a.AsistenciaId,
                        NombreMostrar = a.Usuario != null ? (a.Usuario.Nombre + " " + a.Usuario.ApellidoPaterno).Trim() : "Desconocido",
                        UsuarioNombre = a.Usuario != null ? (a.Usuario.Nombre + " " + a.Usuario.ApellidoPaterno).Trim() : "Desconocido",
                        LaboratorioCodigo = a.Laboratorio.CodigoLaboratorio,
                        Materia = a.Cronograma.Materia,
                        MaquinaCodigo = a.Maquina != null ? a.Maquina.CodigoMaquina : "-",
                        a.HoraIngreso,
                        a.HoraSalida,
                        a.Observacion,
                        a.FechaRegistro,
                        a.Tipo,
                        Rol = a.Usuario != null ? a.Usuario.Rol : "N/A"
                    })
                    .Take(500).ToListAsync();

                return Ok(new { totalAsistencias = data.Count, asistencias = data });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // =============================================================
        // 4. GENERALES
        // =============================================================
        [HttpGet("reportes/asistencias/general")]
        public async Task<IActionResult> ObtenerAsistenciasGeneral()
        {
            try
            {
                var data = await _context.Asistencias
                    .Include(a => a.Usuario).Include(a => a.Laboratorio).Include(a => a.Cronograma).Include(a => a.Maquina)
                    .OrderByDescending(a => a.FechaRegistro).Take(500)
                    .Select(a => new {
                        a.AsistenciaId,
                        UsuarioNombre = a.Usuario != null ? (a.Usuario.Nombre + " " + a.Usuario.ApellidoPaterno).Trim() : "Usuario Eliminado",
                        NombreMostrar = a.Usuario != null ? (a.Usuario.Nombre + " " + a.Usuario.ApellidoPaterno).Trim() : "Usuario Eliminado",
                        Rol = a.Usuario != null ? a.Usuario.Rol : "N/A",
                        LaboratorioCodigo = a.Laboratorio.CodigoLaboratorio,
                        Materia = a.Cronograma != null ? a.Cronograma.Materia : "Libre",
                        MaquinaCodigo = a.Maquina != null ? a.Maquina.CodigoMaquina : "Sin Asignar",
                        a.Tipo,
                        a.RegistroPor,
                        a.HoraIngreso,
                        a.HoraSalida,
                        a.Observacion,
                        a.FechaRegistro
                    }).ToListAsync();
                return Ok(new { totalAsistencias = data.Count, asistencias = data });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("reportes/asistencias/programada")]
        public async Task<IActionResult> ObtenerAsistenciasProgramada()
        {
            try
            {
                var data = await _context.Asistencias.Where(a => a.Tipo == "programada")
                    .Include(a => a.Usuario).Include(a => a.Laboratorio).Include(a => a.Cronograma).Include(a => a.Maquina)
                    .OrderByDescending(a => a.FechaRegistro).Take(500)
                    .Select(a => new {
                        a.AsistenciaId,
                        UsuarioNombre = a.Usuario != null ? (a.Usuario.Nombre + " " + a.Usuario.ApellidoPaterno).Trim() : "Desconocido",
                        NombreMostrar = a.Usuario != null ? (a.Usuario.Nombre + " " + a.Usuario.ApellidoPaterno).Trim() : "Desconocido",
                        Rol = a.Usuario != null ? a.Usuario.Rol : "N/A",
                        LaboratorioCodigo = a.Laboratorio.CodigoLaboratorio,
                        Materia = a.Cronograma.Materia,
                        MaquinaCodigo = a.Maquina != null ? a.Maquina.CodigoMaquina : "-",
                        a.Tipo,
                        a.RegistroPor,
                        a.HoraIngreso,
                        a.HoraSalida,
                        a.Observacion,
                        a.FechaRegistro
                    }).ToListAsync();
                return Ok(new { totalAsistencias = data.Count, asistencias = data });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("reportes/asistencias/uso_libre")]
        public async Task<IActionResult> ObtenerAsistenciasUsoLibre()
        {
            try
            {
                var data = await _context.Asistencias.Where(a => a.Tipo == "uso_libre")
                    .Include(a => a.Usuario).Include(a => a.Laboratorio).Include(a => a.Maquina)
                    .OrderByDescending(a => a.FechaRegistro).Take(500)
                    .Select(a => new {
                        a.AsistenciaId,
                        UsuarioNombre = a.Usuario != null ? (a.Usuario.Nombre + " " + a.Usuario.ApellidoPaterno).Trim() : "Desconocido",
                        NombreMostrar = a.Usuario != null ? (a.Usuario.Nombre + " " + a.Usuario.ApellidoPaterno).Trim() : "Desconocido",
                        Rol = a.Usuario != null ? a.Usuario.Rol : "N/A",
                        LaboratorioCodigo = a.Laboratorio.CodigoLaboratorio,
                        MaquinaCodigo = a.Maquina != null ? a.Maquina.CodigoMaquina : "-",
                        a.Tipo,
                        a.RegistroPor,
                        a.HoraIngreso,
                        a.HoraSalida,
                        a.Observacion,
                        a.FechaRegistro
                    }).ToListAsync();
                return Ok(new { totalAsistencias = data.Count, asistencias = data });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}