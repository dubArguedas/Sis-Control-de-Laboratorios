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
     * ═══════════════════════════════════════════════════════════════════════════════════════
     * 
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
     * GESTIÓN DE MÁQUINAS POR LABORATORIO
     * ───────────────────────────────────────────────────────────────────────────────────────
     * 
     * 2. GET /api/AdminDashboard/laboratorio/{laboratorioId}
     *    Método: ObtenerMaquinasPorLaboratorio()
     *    Propósito: Lista todas las máquinas de un laboratorio con alertas y asignaciones
     *    Parámetros query:
     *      - ordenarPor: "codigo" (default), "estado" o "fecha"
     *    Retorna: Lista completa de máquinas con alertas activas y asistencias en curso
     * 
     * 3. GET /api/AdminDashboard/laboratorio/{laboratorioId}/por-estado/{estado}
     *    Método: ObtenerMaquinasPorEstado()
     *    Propósito: Filtra máquinas por estado específico
     *    Parámetros ruta:
     *      - estado: "disponible", "ocupado", "mantenimiento"
     *    Retorna: Máquinas filtradas por estado
     * 
     * 4. GET /api/AdminDashboard/laboratorio/{laboratorioId}/buscar
     *    Método: BuscarMaquinasPorCodigo()
     *    Propósito: Busca máquinas por código dentro de un laboratorio
     *    Parámetros query:
     *      - codigo: Término de búsqueda (requerido)
     *    Retorna: Máquinas que coinciden con el código buscado
     * 
     * 5. GET /api/AdminDashboard/laboratorio/{laboratorioId}/con-alertas
     *    Método: ObtenerMaquinasConAlertas()
     *    Propósito: Obtiene solo máquinas con alertas pendientes
     *    Retorna: Máquinas con alertas activas y sus detalles
     * 
     * ───────────────────────────────────────────────────────────────────────────────────────
     * DETALLE DE MÁQUINAS INDIVIDUALES
     * ───────────────────────────────────────────────────────────────────────────────────────
     * 
     * 6. GET /api/AdminDashboard/maquina/{maquinaId}
     *    Método: ObtenerDetalleMaquinaBasico()
     *    Propósito: Obtiene información básica de una máquina específica
     *    Retorna: Datos básicos de la máquina y su laboratorio
     * 
     * 7. GET /api/AdminDashboard/maquina/{maquinaId}/historial
     *    Método: ObtenerHistorialCompletomaquina()
     *    Propósito: Historial completo de asistencias y alertas
     *    Parámetros query:
     *      - diasHistorial: Días de historial (default: 30, máx: 365)
     *    Retorna: 
     *      - Últimas 50 asistencias
     *      - Alertas abiertas
     *      - Últimas 20 alertas resueltas
     *      - Métricas: total asistencias, fallas, promedio mensual
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

        // ═══════════════════════════════════════════════════════════════════════════════════
        // 1. RESUMEN DE LABORATORIOS
        // ═══════════════════════════════════════════════════════════════════════════════════
        
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

        // ═══════════════════════════════════════════════════════════════════════════════════
        // 2. GESTIÓN DE MÁQUINAS POR LABORATORIO
        // ═══════════════════════════════════════════════════════════════════════════════════

        [HttpGet("laboratorio/{laboratorioId}")]
        public async Task<IActionResult> ObtenerMaquinasPorLaboratorio(
            int laboratorioId,
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

                var maquinasQuery = _context.Maquinas
                    .Where(m => m.LaboratorioId == laboratorioId
                        && m.Estado.ToLower() != "descontinuado"
                        && m.Estado.ToLower() != "cerrado")
                    .Select(m => new
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

        [HttpGet("laboratorio/{laboratorioId}/por-estado/{estado}")]
        public async Task<IActionResult> ObtenerMaquinasPorEstado(int laboratorioId, string estado)
        {
            try
            {
                var laboratorio = await _context.Laboratorios
                    .Where(l => l.LaboratorioId == laboratorioId)
                    .Select(l => new { l.LaboratorioId, l.CodigoLaboratorio })
                    .FirstOrDefaultAsync();

                if (laboratorio == null)
                    return NotFound(new { message = "Laboratorio no encontrado" });

                var maquinas = await _context.Maquinas
                    .Where(m => m.LaboratorioId == laboratorioId
                        && m.Estado.ToLower() == estado.ToLower()
                        && m.Estado.ToLower() != "descontinuado"
                        && m.Estado.ToLower() != "cerrado")
                    .Select(m => new
                    {
                        m.MaquinaId,
                        m.CodigoMaquina,
                        m.Estado,
                        m.DescripcionHardware,
                        TieneQr = m.Qr != null
                    })
                    .OrderBy(m => m.CodigoMaquina)
                    .ToListAsync();

                return Ok(new
                {
                    laboratorioId = laboratorio.LaboratorioId,
                    codigo = laboratorio.CodigoLaboratorio,
                    estadoFiltrado = estado,
                    total = maquinas.Count,
                    maquinas
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al filtrar máquinas", detail = ex.Message });
            }
        }

        [HttpGet("laboratorio/{laboratorioId}/buscar")]
        public async Task<IActionResult> BuscarMaquinasPorCodigo(
            int laboratorioId,
            [FromQuery] string codigo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigo))
                    return BadRequest(new { message = "Debe proporcionar un código de búsqueda" });

                var maquinas = await _context.Maquinas
                    .Where(m => m.LaboratorioId == laboratorioId
                        && m.CodigoMaquina.Contains(codigo)
                        && m.Estado.ToLower() != "descontinuado"
                        && m.Estado.ToLower() != "cerrado")
                    .Select(m => new
                    {
                        m.MaquinaId,
                        m.CodigoMaquina,
                        m.Estado,
                        m.DescripcionHardware,
                        TieneQr = m.Qr != null
                    })
                    .OrderBy(m => m.CodigoMaquina)
                    .ToListAsync();

                return Ok(new
                {
                    laboratorioId,
                    codigoBuscado = codigo,
                    total = maquinas.Count,
                    maquinas
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al buscar máquinas", detail = ex.Message });
            }
        }

        [HttpGet("laboratorio/{laboratorioId}/con-alertas")]
        public async Task<IActionResult> ObtenerMaquinasConAlertas(int laboratorioId)
        {
            try
            {
                var maquinas = await _context.Maquinas
                    .Where(m => m.LaboratorioId == laboratorioId
                        && m.Alertas!.Any(a => a.EstadoAlerta.ToLower() == "pendiente")
                        && m.Estado.ToLower() != "descontinuado"
                        && m.Estado.ToLower() != "cerrado")
                    .Select(m => new
                    {
                        m.MaquinaId,
                        m.CodigoMaquina,
                        m.Estado,
                        m.DescripcionHardware,
                        AlertasPendientes = m.Alertas!
                            .Where(a => a.EstadoAlerta.ToLower() == "pendiente")
                            .Select(a => new
                            {
                                a.AlertaId,
                                a.Descripcion,
                                a.FechaCreacion
                            })
                            .OrderByDescending(a => a.FechaCreacion)
                            .ToList()
                    })
                    .OrderBy(m => m.CodigoMaquina)
                    .ToListAsync();

                return Ok(new
                {
                    laboratorioId,
                    total = maquinas.Count,
                    maquinas
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener máquinas con alertas", detail = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════════════
        // 3. DETALLE DE MÁQUINAS INDIVIDUALES
        // ═══════════════════════════════════════════════════════════════════════════════════

        [HttpGet("maquina/{maquinaId}")]
        public async Task<IActionResult> ObtenerDetalleMaquinaBasico(int maquinaId)
        {
            try
            {
                var maquina = await _context.Maquinas
                    .Where(m => m.MaquinaId == maquinaId)
                    .Select(m => new
                    {
                        m.MaquinaId,
                        m.CodigoMaquina,
                        m.DescripcionHardware,
                        m.Estado,
                        m.FechaRegistro,
                        TieneQr = m.Qr != null,
                        Laboratorio = new
                        {
                            m.Laboratorio!.LaboratorioId,
                            m.Laboratorio.CodigoLaboratorio,
                            m.Laboratorio.Ubicacion
                        }
                    })
                    .FirstOrDefaultAsync();

                if (maquina == null)
                    return NotFound(new { message = "Máquina no encontrada" });

                return Ok(maquina);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener detalle", detail = ex.Message });
            }
        }

        [HttpGet("maquina/{maquinaId}/historial")]
        public async Task<IActionResult> ObtenerHistorialCompletoMaquina(
            int maquinaId,
            [FromQuery] int diasHistorial = 30)
        {
            try
            {
                if (diasHistorial < 1 || diasHistorial > 365)
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
                            .Where(a => a.FechaRegistro >= DateTime.Now.AddDays(-diasHistorial))
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
                            .Where(a => a.EstadoAlerta.ToLower() == "resuelto" && a.FechaResolucion >= DateTime.Now.AddDays(-diasHistorial))
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
                    .Where(a => a.MaquinaId == maquinaId && a.FechaCreacion >= DateTime.Now.AddDays(-diasHistorial))
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
                        diasConsultados = diasHistorial,
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
                        promedioFallasMensual = Math.Round(totalFallas / (diasHistorial / 30.0), 2)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener historial", detail = ex.Message });
            }
        }
    }
}