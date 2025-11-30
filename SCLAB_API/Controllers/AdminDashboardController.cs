using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCLAB_API.Data;
using SCLAB_API.Models;
using System.Globalization;

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
        // 1. DASHBOARD COMPLETO (ULTIMATE)
        // ═══════════════════════════════════════════════════════════════════════════════════
        
        [HttpGet("resumen-laboratorios")]
        public async Task<IActionResult> ObtenerResumenLaboratorios()
        {
            try
            {
                var hoy = DateTime.Today;
                var inicioSemana = hoy.AddDays(-7);
                var inicioMes = hoy.AddDays(-30);

                // --- 1. DATOS DE LABORATORIOS Y MÁQUINAS ---
                // Incluimos descontinuadas para el reporte completo, solo filtramos "cerrado" si aplica
                var laboratorios = await _context.Laboratorios
                    .Where(l => l.Estado.ToLower() != "cerrado")
                    .Select(l => new
                    {
                        l.LaboratorioId,
                        l.CodigoLaboratorio,
                        l.Ubicacion,
                        Maquinas = l.Maquinas!.Where(m => m.Estado.ToLower() != "cerrado"), // Incluye descontinuado
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
                    int descontinuadas = maquinas.Count(m => m.Estado.ToLower() == "descontinuado");

                    double salud = 100.0;
                    // Salud basada solo en máquinas operativas (total - descontinuadas)
                    int totalOperativo = total - descontinuadas;
                    if (totalOperativo > 0)
                    {
                        salud = 100 - ((mantenimiento / (double)totalOperativo) * 50) - ((lab.AlertasPendientes / (double)totalOperativo) * 50);
                        salud = Math.Max(0, salud);
                    }

                    return new
                    {
                        laboratorioId = lab.LaboratorioId,
                        codigo = lab.CodigoLaboratorio,
                        ubicacion = lab.Ubicacion,
                        totalMaquinas = total,
                        estados = new { disponibles, ocupadas, mantenimiento, descontinuadas },
                        alertasPendientes = lab.AlertasPendientes,
                        saludPorcentaje = Math.Round(salud, 2),
                        ultimaActualizacion = lab.UltimaAlertaFecha != default ? lab.UltimaAlertaFecha : DateTime.Now
                    };
                }).ToList();

                var totalLaboratorios = laboratorios.Count;
                var totalMaquinas = resumenLaboratorios.Sum(r => r.totalMaquinas);
                
                // Desglose detallado para gráficas
                var totalMaquinasDisponibles = resumenLaboratorios.Sum(r => r.estados.disponibles);
                var totalMaquinasOcupadas = resumenLaboratorios.Sum(r => r.estados.ocupadas);
                var totalMaquinasMantenimiento = resumenLaboratorios.Sum(r => r.estados.mantenimiento);
                var totalMaquinasDescontinuadas = resumenLaboratorios.Sum(r => r.estados.descontinuadas);
                
                var totalMaquinasOperativas = totalMaquinasDisponibles + totalMaquinasOcupadas;

                // --- 2. MÉTRICAS DE USUARIOS ---
                var totalUsuarios = await _context.Usuarios.CountAsync(u => u.Estado == "activo");
                var nuevosUsuariosMes = await _context.Usuarios.CountAsync(u => u.FechaRegistro >= inicioMes);
                
                var usuariosPorRol = await _context.Usuarios
                    .Where(u => u.Estado == "activo")
                    .GroupBy(u => u.Rol)
                    .Select(g => new { Rol = g.Key, Cantidad = g.Count() })
                    .ToDictionaryAsync(k => k.Rol, v => v.Cantidad);

                // --- 3. MÉTRICAS DE ASISTENCIA ---
                var asistenciasActivas = await _context.Asistencias.CountAsync(a => a.HoraSalida == null);
                var asistenciasHoy = await _context.Asistencias.CountAsync(a => a.FechaRegistro.Date == hoy);
                var asistenciasSemana = await _context.Asistencias.CountAsync(a => a.FechaRegistro >= inicioSemana);

                // Gráfico: Asistencias por hora (Hoy)
                var asistenciasPorHoraData = await _context.Asistencias
                    .Where(a => a.FechaRegistro.Date == hoy)
                    .GroupBy(a => a.HoraIngreso.Hour)
                    .Select(g => new { Hora = g.Key, Cantidad = g.Count() })
                    .OrderBy(x => x.Hora)
                    .ToListAsync();

                // Llenar huecos de horas
                var asistenciasPorHora = Enumerable.Range(7, 14) // 7 AM a 8 PM
                    .Select(h => new 
                    { 
                        Hora = $"{h}:00", 
                        Cantidad = asistenciasPorHoraData.FirstOrDefault(x => x.Hora == h)?.Cantidad ?? 0 
                    })
                    .ToList();

                // --- 4. MÉTRICAS DE SOPORTE (ALERTAS) ---
                var alertasPendientes = await _context.Alertas.CountAsync(a => a.EstadoAlerta.ToLower() == "pendiente");
                var alertasResueltasHoy = await _context.Alertas.CountAsync(a => a.EstadoAlerta.ToLower() == "resuelto" && a.FechaResolucion.HasValue && a.FechaResolucion.Value.Date == hoy);

                // Lista: Alertas Recientes
                var alertasRecientes = await _context.Alertas
                    .Where(a => a.EstadoAlerta.ToLower() == "pendiente")
                    .OrderByDescending(a => a.FechaCreacion)
                    .Take(5)
                    .Select(a => new 
                    {
                        a.AlertaId,
                        a.Descripcion,
                        Laboratorio = a.Laboratorio!.CodigoLaboratorio,
                        Maquina = a.Maquina!.CodigoMaquina,
                        Hace = (DateTime.Now - a.FechaCreacion).TotalMinutes < 60 
                            ? $"{(int)(DateTime.Now - a.FechaCreacion).TotalMinutes} min" 
                            : $"{(int)(DateTime.Now - a.FechaCreacion).TotalHours} hrs"
                    })
                    .ToListAsync();

                // --- 5. MÉTRICAS DE USO (NUEVO) ---
                // Capacidad teórica: 24 horas por máquina OPERATIVA por día
                double horasOperativasDia = 24.0;
                // Excluimos descontinuadas y mantenimiento de la capacidad teórica ideal? 
                // Generalmente capacidad = maquinas * 24h. Si están en mantenimiento, afecta la disponibilidad (es uso perdido).
                // Pero descontinuadas NO deberían contar en la capacidad.
                double capacidadDiariaHoras = (totalMaquinas - totalMaquinasDescontinuadas) * horasOperativasDia;
                double capacidadSemanalHoras = capacidadDiariaHoras * 7;

                // Uso Diario (Hoy)
                var asistenciasHoyLista = await _context.Asistencias
                    .Where(a => a.FechaRegistro.Date == hoy)
                    .Select(a => new { a.HoraIngreso, a.HoraSalida })
                    .ToListAsync();

                double horasUsoHoy = asistenciasHoyLista.Sum(a => 
                    (a.HoraSalida ?? DateTime.Now).Subtract(a.HoraIngreso).TotalHours);

                // Uso Semanal (Últimos 7 días)
                var asistenciasSemanaLista = await _context.Asistencias
                    .Where(a => a.FechaRegistro >= inicioSemana)
                    .Select(a => new { a.HoraIngreso, a.HoraSalida })
                    .ToListAsync();

                double horasUsoSemana = asistenciasSemanaLista.Sum(a => 
                    (a.HoraSalida ?? DateTime.Now).Subtract(a.HoraIngreso).TotalHours);

                double porcentajeUsoDiario = capacidadDiariaHoras > 0 ? (horasUsoHoy / capacidadDiariaHoras) * 100 : 0;
                double porcentajeUsoSemanal = capacidadSemanalHoras > 0 ? (horasUsoSemana / capacidadSemanalHoras) * 100 : 0;

                // --- CONSTRUCCIÓN DE RESPUESTA ---
                return Ok(new
                {
                    // Infraestructura
                    totalLaboratorios,
                    totalMaquinas,
                    totalMaquinasOperativas,
                    totalMaquinasDisponibles,
                    totalMaquinasOcupadas,
                    totalMaquinasMantenimiento,
                    totalMaquinasDescontinuadas, // Nuevo
                    laboratorios = resumenLaboratorios,

                    // Usuarios
                    totalUsuarios,
                    nuevosUsuariosMes,
                    usuariosPorRol = new 
                    {
                        admin = usuariosPorRol.ContainsKey("admin") ? usuariosPorRol["admin"] : 0,
                        encargado = usuariosPorRol.ContainsKey("encargado") ? usuariosPorRol["encargado"] : 0,
                        docente = usuariosPorRol.ContainsKey("docente") ? usuariosPorRol["docente"] : 0,
                        estudiante = usuariosPorRol.ContainsKey("estudiante") ? usuariosPorRol["estudiante"] : 0
                    },

                    // Asistencia
                    asistenciasActivas,
                    asistenciasHoy,
                    asistenciasSemana,
                    chartAsistencias = asistenciasPorHora,

                    // Soporte
                    alertasPendientes,
                    alertasResueltasHoy,
                    alertasRecientes,

                    // Uso
                    porcentajeUsoDiario = Math.Round(porcentajeUsoDiario, 1),
                    porcentajeUsoSemanal = Math.Round(porcentajeUsoSemanal, 1)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener el dashboard", detail = ex.Message });
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
                        && m.Estado.ToLower() != "cerrado") // Mostramos todas, incluso descontinuadas
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