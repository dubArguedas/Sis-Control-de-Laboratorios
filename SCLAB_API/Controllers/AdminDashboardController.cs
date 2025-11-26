using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCLAB_API.Data;
using SCLAB_API.Models;

namespace SCLAB_API.Controllers
{
    /*
     * ═══════════════════════════════════════════════════════════════════════════════════════
     * ADMIN DASHBOARD CONTROLLER - ÍNDICE DE ENDPOINTS
     * ══════════════════════════════════════════════════════════════════════════════════════=
     * ───────────────────────────────────────────────────────────────────────────────────────
     * RESUMEN DE LABORATORIOS
     * ───────────────────────────────────────────────────────────────────────────────────────
     * 
     * 1. GET /api/AdminDashboard/resumen-laboratorios
     *    Método: ObtenerResumenLaboratorios()
     *    Propósito: Obtiene resumen global de todos los laboratorios activos
     *    Retorna: 
     *      - Totales globales (laboratorios, máquinas, máquinas en falla)
     *      - Por cada laboratorio:
     *          · Estados de máquinas (disponibles, ocupadas, mantenimiento)
     *          · Alertas pendientes
     *          · Indicador de salud (%)
     *          · Última actualización
     * 
     * ───────────────────────────────────────────────────────────────────────────────────────
     * GESTIÓN DE MÁQUINAS
     * ───────────────────────────────────────────────────────────────────────────────────────
     * 
     * 2. GET /api/AdminDashboard/laboratorio/{laboratorioId}/maquinas
     *    Método: ObtenerMaquinasLaboratorio()
     *    Propósito: Lista máquinas de un laboratorio con filtros y ordenamiento
     *    Parámetros query:
     *      - filtroEstadoMaquina: Filtro por estado (disponible, ocupado, mantenimiento)
     *      - busquedaCodigoMaquina: Búsqueda por código de máquina
     *      - mostrarSoloConAlertasPendientes: Mostrar solo máquinas con alertas pendientes
     *      - ordenarPor: Ordenar por "codigo", "estado" o "fecha"
     *    Retorna: Lista de máquinas con alertas activas y asignaciones actuales
     * 
     * 3. GET /api/AdminDashboard/maquina/{maquinaId}/detalle
     *    Método: ObtenerDetalleMaquina()
     *    Propósito: Detalle completo de una máquina con historial
     *    Parámetros query:
     *      - diasHistorialConsulta: Días de historial a consultar (default: 30)
     *    Retorna:
     *      - Información básica de la máquina
     *      - Historial de asistencias (últimas 50)
     *      - Alertas abiertas y resueltas
     *      - Métricas: total asistencias, fallas, promedio mensual
     * 
     * ───────────────────────────────────────────────────────────────────────────────────────
     * GESTIÓN DE ALERTAS 
     * ───────────────────────────────────────────────────────────────────────────────────────
     * 
     * 4. POST /api/AdminDashboard/alerta
     *    Método: CrearAlerta()
     *    Propósito: Crea una nueva alerta para una máquina
     *    Body (CrearAlertaDto):
     *      - MaquinaId: ID de la máquina
     *      - UsuarioId: ID del usuario que crea la alerta
     *      - Descripcion: Descripción del problema
     *      - CambiarEstadoMaquina: Si se debe cambiar a "mantenimiento" (default: true)
     *    Retorna: ID de la alerta creada y fecha de creación
     * 
     * 5. GET /api/AdminDashboard/alertas
     *    Método: ListarAlertas()
     *    Propósito: Lista alertas con múltiples filtros
     *    Parámetros query:
     *      - filtroEstadoAlerta: Filtro por estado (pendiente, resuelto)
     *      - filtroLaboratorioId: Filtro por laboratorio
     *      - filtroMaquinaId: Filtro por máquina
     *      - filtroFechaCreacionDesde: Fecha inicio del rango
     *      - filtroFechaCreacionHasta: Fecha fin del rango
     *    Retorna: Máximo 100 alertas ordenadas por fecha de creación (desc)
     * 
     * 6. PUT /api/AdminDashboard/alerta/{alertaId}/resolver
     *    Método: ResolverAlerta()
     *    Propósito: Resuelve una alerta pendiente y actualiza estado de máquina
     *    Body (ResolverAlertaDto):
     *      - UsuarioId: ID del usuario que resuelve
     *      - TipoSolucion: Tipo de solución aplicada
     *      - DescripcionSolucion: Descripción de la solución
     *      - EstadoMaquinaDespues: Estado posterior ("disponible" o "mantenimiento")
     *    Retorna: Confirmación con ID de alerta y fecha de resolución
     * 
     * 7. GET /api/AdminDashboard/alertas/pendientes/contador
     *    Método: ObtenerContadorAlertasPendientes()
     *    Propósito: Contador simple de alertas pendientes
     *    Parámetros query:
     *      - filtroLaboratorioId: Opcional, filtra por laboratorio
     *    Retorna: Total de alertas pendientes
     * 
     * ───────────────────────────────────────────────────────────────────────────────────────
     * REPORTES DE ASISTENCIAS 
     * ───────────────────────────────────────────────────────────────────────────────────────
     * 
     * 8. GET /api/AdminDashboard/reportes/maquinas/distribucion-estados/{laboratorioId}
     *     Método: ObtenerDistribucionEstados()
     *     Propósito: Distribución actual de estados de máquinas (para gráfico de torta)
     *     Retorna: Por cada estado activo: cantidad y porcentaje
     *     
     * 9. GET /api/AdminDashboard/reportes/asistencias/{nombreMateria}
     *    Método: ObtenerAsistenciasPorMateria()
     *    Propósito: Busca asistencias de docentes por nombre de materia
     * 
     * 10. GET /api/AdminDashboard/reportes/asistencias/busqueda/{nombreMateria}
     *     Método: ObtenerAsistenciasEstudiantesporMateria()
     *     Propósito: Busca asistencias de ESTUDIANTES por nombre de materia
     * 
     * 11. GET /api/AdminDashboard/reportes/asistencias/horario/{diaSemana}/{horaInicioClase}/{horaFinClase}
     *     Método: ObtenerAsistenciasporHorario()
     *     Propósito: Busca asistencias de ESTUDIANTES por día y rango horarios
     * 12. GET /api/AdminDashboard/reportes/asistencias/general
     *     Método: ObtenerAsistenciasGeneral()
     *     Propósito: Obtiene todas las asistencias del sistema (docentes y estudiantes)
     * 13. GET /api/AdminDashboard/reportes/asistencias/uso_libre
     *     Método: ObtenerAsistenciasUso_libre()
     *     Propósito: Obtiene todas las asistencias del sistema (docentes y estudiantes) filtrados por uso libre
     * 14. GET /api/AdminDashboard/reportes/asistencias/programada
     *     Método: ObtenerAsistenciasProgramada()
     *     Propósito: Obtiene todas las asistencias del sistema (docentes y estudiantes) filtrados por asistencias programadas
     * 
     * ───────────────────────────────────────────────────────────────────────────────────────
     */
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "admin,encargado")] 
    public class AdminDashboardController : ControllerBase
    {
        private readonly SisComputoDbContext _context;

        public AdminDashboardController(SisComputoDbContext context)
        {
            _context = context;
        }

        [HttpGet("resumen-laboratorios")]
        public async Task<IActionResult> ObtenerResumenLaboratorios()
        {
            try
            {
                var laboratorios = await _context.Laboratorios
                    .Where(l => l.Estado.ToLower() != "cerrado")
                    .Select(l => new
                    {
                        l.LaboratorioId,
                        l.CodigoLaboratorio,
                        l.Ubicacion,
                        Maquinas = l.Maquinas!.Where(m => m.Estado.ToLower() != "descontinuado" && m.Estado.ToLower() != "cerrado"),
                        AlertasPendientes = l.Alertas!.Count(a => a.EstadoAlerta.ToLower() == "pendiente"),
                        UltimaAlertaFecha = l.Alertas!.OrderByDescending(a => a.FechaCreacion).Select(a => a.FechaCreacion).FirstOrDefault()
                    })
                    .ToListAsync();

                var resumenLaboratorios = laboratorios.Select(lab =>
                {
                    var maquinas = lab.Maquinas.ToList();
                    int total = maquinas.Count;
                    int disponibles = maquinas.Count(m => m.Estado.ToLower() == "disponible" || m.Estado.ToLower() == "libre");
                    int ocupadas = maquinas.Count(m => m.Estado.ToLower() == "ocupado");
                    int mantenimiento = maquinas.Count(m => m.Estado.ToLower() == "mantenimiento");

                    double salud = 100.0;
                    if (total > 0)
                    {
                        salud = 100 - ((mantenimiento / (double)total) * 50) - ((lab.AlertasPendientes / (double)total) * 50);
                        salud = Math.Max(0, salud);
                    }

                    return new
                    {
                        laboratorioId = lab.LaboratorioId,
                        codigo = lab.CodigoLaboratorio,
                        ubicacion = lab.Ubicacion,
                        totalMaquinas = total,
                        estados = new { disponibles, ocupadas, mantenimiento },
                        alertasPendientes = lab.AlertasPendientes,
                        saludPorcentaje = Math.Round(salud, 2),
                        ultimaActualizacion = lab.UltimaAlertaFecha != default ? lab.UltimaAlertaFecha : DateTime.Now
                    };
                }).ToList();

                var totalMaquinas = resumenLaboratorios.Sum(r => r.totalMaquinas);
                var totalMantenimiento = resumenLaboratorios.Sum(r => r.estados.mantenimiento);

                return Ok(new
                {
                    totalLaboratorios = laboratorios.Count,
                    totalMaquinas,
                    totalMaquinasEnFalla = totalMantenimiento,
                    porcentajeFalla = totalMaquinas > 0 ? Math.Round((totalMantenimiento / (double)totalMaquinas) * 100, 2) : 0,
                    laboratorios = resumenLaboratorios
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener el resumen", detail = ex.Message });
            }
        }

        [HttpGet("laboratorio/{laboratorioId}/maquinas")]
        public async Task<IActionResult> ObtenerMaquinasLaboratorio(
            int laboratorioId,
            [FromQuery] string? filtroEstadoMaquina = null,
            [FromQuery] string? busquedaCodigoMaquina = null,
            [FromQuery] bool? mostrarSoloConAlertasPendientes = null,
            [FromQuery] string? ordenarPor = "codigo")
        {
            try
            {
                var laboratorio = await _context.Laboratorios
                    .Where(l => l.LaboratorioId == laboratorioId)
                    .Select(l => new { l.LaboratorioId, l.CodigoLaboratorio })
                    .FirstOrDefaultAsync();

                if (laboratorio == null)
                    return NotFound(new { message = "Laboratorio no encontrado" });

                var query = _context.Maquinas
                    .Where(m => m.LaboratorioId == laboratorioId
                        && m.Estado.ToLower() != "descontinuado"
                        && m.Estado.ToLower() != "cerrado")
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(filtroEstadoMaquina))
                    query = query.Where(m => m.Estado.ToLower() == filtroEstadoMaquina.ToLower());

                if (!string.IsNullOrWhiteSpace(busquedaCodigoMaquina))
                    query = query.Where(m => m.CodigoMaquina.Contains(busquedaCodigoMaquina));

                if (mostrarSoloConAlertasPendientes == true)
                    query = query.Where(m => m.Alertas!.Any(a => a.EstadoAlerta.ToLower() == "pendiente"));

                var maquinasQuery = query.Select(m => new
                {
                    m.MaquinaId,
                    m.CodigoMaquina,
                    m.Estado,
                    m.DescripcionHardware,
                    m.FechaRegistro,
                    TieneQr = m.Qr != null,
                    AlertaActiva = m.Alertas!
                        .Where(a => a.EstadoAlerta.ToLower() == "pendiente")
                        .OrderByDescending(a => a.FechaCreacion)
                        .Select(a => new { a.AlertaId, a.Descripcion })
                        .FirstOrDefault(),
                    AsistenciaActiva = m.Asistencias!
                        .Where(a => a.HoraSalida == null)
                        .OrderByDescending(a => a.HoraIngreso)
                        .Select(a => new
                        {
                            a.UsuarioId,
                            UsuarioNombre = a.Usuario!.Nombre + " " + a.Usuario.ApellidoPaterno,
                            a.HoraIngreso
                        })
                        .FirstOrDefault()
                });

                var maquinas = await (ordenarPor?.ToLower() switch
                {
                    "estado" => maquinasQuery.OrderBy(m => m.Estado),
                    "fecha" => maquinasQuery.OrderByDescending(m => m.FechaRegistro),
                    _ => maquinasQuery.OrderBy(m => m.CodigoMaquina)
                }).ToListAsync();

                var resultado = maquinas.Select(m => new
                {
                    m.MaquinaId,
                    m.CodigoMaquina,
                    m.Estado,
                    m.DescripcionHardware,
                    m.TieneQr,
                    tiempoDesdeRegistro = new
                    {
                        dias = (DateTime.Now - m.FechaRegistro).Days,
                        horas = (DateTime.Now - m.FechaRegistro).Hours
                    },
                    alerta = m.AlertaActiva != null ? new
                    {
                        m.AlertaActiva.AlertaId,
                        m.AlertaActiva.Descripcion
                    } : null,
                    asignacion = m.AsistenciaActiva != null ? new
                    {
                        m.AsistenciaActiva.UsuarioId,
                        m.AsistenciaActiva.UsuarioNombre,
                        tiempoTranscurrido = (int)(DateTime.Now - m.AsistenciaActiva.HoraIngreso).TotalMinutes
                    } : null
                });

                return Ok(new
                {
                    laboratorioId = laboratorio.LaboratorioId,
                    codigo = laboratorio.CodigoLaboratorio,
                    total = resultado.Count(),
                    maquinas = resultado
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener las máquinas", detail = ex.Message });
            }
        }

        [HttpGet("maquina/{maquinaId}/detalle")]
        public async Task<IActionResult> ObtenerDetalleMaquina(
            int maquinaId, 
            [FromQuery] int diasHistorialConsulta = 30)
        {
            try
            {
                
                if (diasHistorialConsulta < 1 || diasHistorialConsulta > 365)
                    return BadRequest(new { message = "El historial debe estar entre 1 y 365 días" });

                var maquina = await _context.Maquinas
                    .Where(m => m.MaquinaId == maquinaId)
                    .Select(m => new
                    {
                        m.MaquinaId,
                        m.CodigoMaquina,
                        m.DescripcionHardware,
                        m.Estado,
                        m.FechaRegistro,
                        Laboratorio = new
                        {
                            m.Laboratorio!.LaboratorioId,
                            m.Laboratorio.CodigoLaboratorio,
                            m.Laboratorio.Ubicacion
                        },
                        Asistencias = m.Asistencias!
                            .Where(a => a.FechaRegistro >= DateTime.Now.AddDays(-diasHistorialConsulta))
                            .OrderByDescending(a => a.FechaRegistro)
                            .Take(50)
                            .Select(a => new
                            {
                                a.FechaRegistro,
                                a.HoraIngreso,
                                a.HoraSalida,
                                Usuario = a.Usuario!.Nombre + " " + a.Usuario.ApellidoPaterno,
                                a.Usuario.Rol,
                                a.Observacion
                            }),
                        AlertasAbiertas = m.Alertas!
                            .Where(a => a.EstadoAlerta.ToLower() == "pendiente")
                            .Select(a => new
                            {
                                a.AlertaId,
                                a.Descripcion,
                                a.FechaCreacion,
                                CreadaPor = a.UsuarioCreador!.Nombre + " " + a.UsuarioCreador.ApellidoPaterno
                            }),
                        AlertasResueltas = m.Alertas!
                            .Where(a => a.EstadoAlerta.ToLower() == "resuelto" && a.FechaResolucion >= DateTime.Now.AddDays(-diasHistorialConsulta))
                            .OrderByDescending(a => a.FechaResolucion)
                            .Take(20)
                            .Select(a => new
                            {
                                a.AlertaId,
                                a.Descripcion,
                                a.FechaCreacion,
                                a.FechaResolucion,
                                a.SolucionTipo,
                                a.SolucionDescripcion,
                                ResueltaPor = a.UsuarioResolutor != null ? a.UsuarioResolutor.Nombre + " " + a.UsuarioResolutor.ApellidoPaterno : null
                            })
                    })
                    .FirstOrDefaultAsync();

                if (maquina == null)
                    return NotFound(new { message = "Máquina no encontrada" });

                var totalFallas = await _context.Alertas
                    .Where(a => a.MaquinaId == maquinaId && a.FechaCreacion >= DateTime.Now.AddDays(-diasHistorialConsulta))
                    .CountAsync();

                return Ok(new
                {
                    maquina = new
                    {
                        maquina.MaquinaId,
                        maquina.CodigoMaquina,
                        maquina.DescripcionHardware,
                        maquina.Estado,
                        maquina.FechaRegistro,
                        maquina.Laboratorio
                    },
                    historial = new
                    {
                        diasConsultados = diasHistorialConsulta,
                        asistencias = maquina.Asistencias
                    },
                    alertas = new
                    {
                        abiertas = maquina.AlertasAbiertas,
                        resueltasRecientes = maquina.AlertasResueltas
                    },
                    metricas = new
                    {
                        totalAsistencias = maquina.Asistencias.Count(),
                        totalFallas = totalFallas,
                        promedioFallasMensual = Math.Round(totalFallas / (diasHistorialConsulta / 30.0), 2)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener el detalle", detail = ex.Message });
            }
        }

        [HttpPost("alerta")]
        public async Task<IActionResult> CrearAlerta([FromBody] CrearAlertaDto dto)
        {
            try
            {
                
                if (string.IsNullOrWhiteSpace(dto.Descripcion) || dto.Descripcion.Length < 10)
                    return BadRequest(new { message = "La descripción debe tener al menos 10 caracteres" });

                var maquina = await _context.Maquinas
                    .Where(m => m.MaquinaId == dto.MaquinaId)
                    .Select(m => new { m.MaquinaId, m.LaboratorioId, m.Estado })
                    .FirstOrDefaultAsync();

                if (maquina == null)
                    return NotFound(new { message = "Máquina no encontrada" });

                if (!await _context.Usuarios.AnyAsync(u => u.UsuarioId == dto.UsuarioId))
                    return NotFound(new { message = "Usuario no encontrado" });

                var alerta = new Alerta
                {
                    MaquinaId = dto.MaquinaId,
                    LaboratorioId = maquina.LaboratorioId,
                    CreadaPor = dto.UsuarioId,
                    Descripcion = dto.Descripcion,
                    EstadoAlerta = "pendiente",
                    FechaCreacion = DateTime.Now
                };

                _context.Alertas.Add(alerta);

                if (dto.CambiarEstadoMaquina)
                {
                    var maquinaEntity = await _context.Maquinas.FindAsync(dto.MaquinaId);
                    if (maquinaEntity != null)
                        maquinaEntity.Estado = "mantenimiento";
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Alerta creada exitosamente",
                    alertaId = alerta.AlertaId,
                    fechaCreacion = alerta.FechaCreacion
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al crear la alerta", detail = ex.Message });
            }
        }

        [HttpGet("alertas")]
        public async Task<IActionResult> ListarAlertas(
            [FromQuery] string? filtroEstadoAlerta = null,
            [FromQuery] int? filtroLaboratorioId = null,
            [FromQuery] int? filtroMaquinaId = null,
            [FromQuery] DateTime? filtroFechaCreacionDesde = null,
            [FromQuery] DateTime? filtroFechaCreacionHasta = null)
        {
            try
            {
                var query = _context.Alertas.AsQueryable();

                if (!string.IsNullOrWhiteSpace(filtroEstadoAlerta))
                    query = query.Where(a => a.EstadoAlerta.ToLower() == filtroEstadoAlerta.ToLower());

                if (filtroLaboratorioId.HasValue)
                    query = query.Where(a => a.LaboratorioId == filtroLaboratorioId.Value);

                if (filtroMaquinaId.HasValue)
                    query = query.Where(a => a.MaquinaId == filtroMaquinaId.Value);

                if (filtroFechaCreacionDesde.HasValue)
                    query = query.Where(a => a.FechaCreacion >= filtroFechaCreacionDesde.Value);

                if (filtroFechaCreacionHasta.HasValue)
                    query = query.Where(a => a.FechaCreacion <= filtroFechaCreacionHasta.Value);

                var alertas = await query
                    .OrderByDescending(a => a.FechaCreacion)
                    .Select(a => new
                    {
                        a.AlertaId,
                        maquina = new { a.MaquinaId, Codigo = a.Maquina!.CodigoMaquina },
                        laboratorio = new { a.LaboratorioId, Codigo = a.Laboratorio!.CodigoLaboratorio },
                        a.Descripcion,
                        a.EstadoAlerta,
                        a.FechaCreacion,
                        a.FechaResolucion,
                        creadaPor = a.UsuarioCreador!.Nombre + " " + a.UsuarioCreador.ApellidoPaterno,
                        resueltaPor = a.UsuarioResolutor != null ? a.UsuarioResolutor.Nombre + " " + a.UsuarioResolutor.ApellidoPaterno : null,
                        a.SolucionTipo
                    })
                    .Take(100)
                    .ToListAsync();

                return Ok(new { total = alertas.Count, alertas });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al listar alertas", detail = ex.Message });
            }
        }

        [HttpPut("alerta/{alertaId}/resolver")]
        public async Task<IActionResult> ResolverAlerta(int alertaId, [FromBody] ResolverAlertaDto dto)
        {
            try
            {
                
                if (string.IsNullOrWhiteSpace(dto.TipoSolucion))
                    return BadRequest(new { message = "Debe especificar el tipo de solución" });

                if (string.IsNullOrWhiteSpace(dto.DescripcionSolucion) || dto.DescripcionSolucion.Length < 10)
                    return BadRequest(new { message = "La descripción de la solución debe tener al menos 10 caracteres" });

                var alerta = await _context.Alertas
                    .Include(a => a.Maquina)
                    .FirstOrDefaultAsync(a => a.AlertaId == alertaId);

                if (alerta == null)
                    return NotFound(new { message = "Alerta no encontrada" });

                if (alerta.EstadoAlerta.ToLower() == "resuelto")
                    return BadRequest(new { message = "La alerta ya está resuelta" });

                if (!await _context.Usuarios.AnyAsync(u => u.UsuarioId == dto.UsuarioId))
                    return NotFound(new { message = "Usuario no encontrado" });

                alerta.EstadoAlerta = "resuelto";
                alerta.FechaResolucion = DateTime.Now;
                alerta.ResueltoPor = dto.UsuarioId;
                alerta.SolucionTipo = dto.TipoSolucion;
                alerta.SolucionDescripcion = dto.DescripcionSolucion;

                if (alerta.Maquina != null)
                {
                    alerta.Maquina.Estado = dto.EstadoMaquinaDespues.ToLower() switch
                    {
                        "disponible" => "disponible",
                        "mantenimiento" => "mantenimiento",
                        _ => "disponible"
                    };
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Alerta resuelta exitosamente",
                    alertaId = alerta.AlertaId,
                    fechaResolucion = alerta.FechaResolucion
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al resolver la alerta", detail = ex.Message });
            }
        }

        [HttpGet("alertas/pendientes/contador")]
        public async Task<IActionResult> ObtenerContadorAlertasPendientes(
            [FromQuery] int? filtroLaboratorioId = null)
        {
            try
            {
                var query = _context.Alertas.Where(a => a.EstadoAlerta.ToLower() == "pendiente");

                if (filtroLaboratorioId.HasValue)
                    query = query.Where(a => a.LaboratorioId == filtroLaboratorioId.Value);

                var total = await query.CountAsync();

                return Ok(new { totalPendientes = total });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener contador", detail = ex.Message });
            }
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
                    .OrderByDescending(a => a.FechaRegistro)
                    .Take(500)
                    .Where(a => a.Tipo == "programada")
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

        public class CrearAlertaDto
        {
            public int MaquinaId { get; set; }
            public int UsuarioId { get; set; }
            public string Descripcion { get; set; } = string.Empty;
            public bool CambiarEstadoMaquina { get; set; } = true;
        }

        public class ResolverAlertaDto
        {
            public int UsuarioId { get; set; }
            public string TipoSolucion { get; set; } = string.Empty;
            public string DescripcionSolucion { get; set; } = string.Empty;
            public string EstadoMaquinaDespues { get; set; } = "disponible";
        }
    }
}