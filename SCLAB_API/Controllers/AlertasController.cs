using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SCLAB_API.Data;
using SCLAB_API.Models;
using SCLAB_API.Services;

namespace SCLAB_API.Controllers
{
    /*
     * ═══════════════════════════════════════════════════════════════════════════════════════
     * CONTROLADOR DE ALERTAS - ÍNDICE DE ENDPOINTS
     * ═══════════════════════════════════════════════════════════════════════════════════════
     * 
     * ───────────────────────────────────────────────────────────────────────────────────────
     * CREAR Y RESOLVER ALERTAS
     * ───────────────────────────────────────────────────────────────────────────────────────
     * 
     * 1. POST /api/Alertas/alerta
     *    Método: CrearAlerta()
     *    Propósito: Crea una nueva alerta para una máquina
     *    Body (CrearAlertaDto):
     *      - MaquinaId: ID de la máquina
     *      - UsuarioId: ID del usuario que crea la alerta
     *      - Descripcion: Descripción del problema (mínimo 10 caracteres)
     *      - CambiarEstadoMaquina: Si cambiar estado a "mantenimiento" (default: true)
     *    Retorna: ID de la alerta creada y fecha de creación
     * 
     * 2. PUT /api/Alertas/alerta/{alertaId}/resolver
     *    Método: ResolverAlerta()
     *    Propósito: Resuelve una alerta pendiente y actualiza estado de máquina
     *    Body (ResolverAlertaDto):
     *      - UsuarioId: ID del usuario que resuelve
     *      - TipoSolucion: Tipo de solución aplicada
     *      - DescripcionSolucion: Descripción de la solución (mínimo 10 caracteres)
     *      - EstadoMaquinaDespues: "disponible" o "mantenimiento" (default: "disponible")
     *    Retorna: Confirmación con ID de alerta y fecha de resolución
     * 
     * ───────────────────────────────────────────────────────────────────────────────────────
     * CONSULTAR ALERTAS
     * ───────────────────────────────────────────────────────────────────────────────────────
     * 
     * 3. GET /api/Alertas/alertas
     *    Método: ObtenerTodasAlertas()
     *    Propósito: Lista todas las alertas del sistema (máximo 100)
     *    Retorna: Alertas ordenadas por fecha de creación descendente
     * 
     * 4. GET /api/Alertas/alertas/estado/{estado}
     *    Método: ObtenerAlertasPorEstado()
     *    Propósito: Filtra alertas por estado específico
     *    Parámetros ruta:
     *      - estado: "pendiente" o "resuelto"
     *    Retorna: Máximo 100 alertas del estado solicitado
     * 
     * 5. GET /api/Alertas/alertas/laboratorio/{laboratorioId}
     *    Método: ObtenerAlertasPorLaboratorio()
     *    Propósito: Filtra alertas por laboratorio
     *    Parámetros ruta:
     *      - laboratorioId: ID del laboratorio
     *    Retorna: Máximo 100 alertas del laboratorio
     * 
     * 6. GET /api/Alertas/alertas/maquina/{maquinaId}
     *    Método: ObtenerAlertasPorMaquina()
     *    Propósito: Filtra alertas por máquina específica
     *    Parámetros ruta:
     *      - maquinaId: ID de la máquina
     *    Retorna: Máximo 100 alertas de la máquina
     * 
     * 7. GET /api/Alertas/alertas/rango-fechas
     *    Método: ObtenerAlertasPorRangoFechas()
     *    Propósito: Filtra alertas por rango de fechas
     *    Parámetros query:
     *      - fechaDesde: Fecha inicio (requerido)
     *      - fechaHasta: Fecha fin (requerido)
     *    Retorna: Máximo 100 alertas en el rango especificado
     * 
     * ───────────────────────────────────────────────────────────────────────────────────────
     * CONTADORES Y ESTADÍSTICAS
     * ───────────────────────────────────────────────────────────────────────────────────────
     * 
     * 8. GET /api/Alertas/alertas/pendientes/contador
     *    Método: ObtenerContadorAlertasPendientes()
     *    Propósito: Contador de alertas pendientes (global o por laboratorio)
     *    Parámetros query:
     *      - filtroLaboratorioId: ID del laboratorio (opcional)
     *    Retorna: Total de alertas pendientes
     */
    [Route("api/[controller]")]
    [ApiController]
    public class AlertasController : ControllerBase
    {
        private readonly SisComputoDbContext _context;
        private readonly IHubContext<AlertasHub, IAlertasClient> _hubContext;

        public AlertasController(SisComputoDbContext context, IHubContext<AlertasHub, IAlertasClient> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
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
                await _hubContext.Clients.All.RecibirAlerta($"Nueva alerta en máquina {dto.MaquinaId}");
                // Más adelante enviaremos objetos completos:
                // await _hubContext.Clients.All.RecibirNuevaAlerta(nuevaAlertaMapeada);
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
        public async Task<IActionResult> ObtenerTodasAlertas()
        {
            try
            {
                var alertas = await _context.Alertas
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

        // 2. Filtrar alertas por ESTADO
        [HttpGet("alertas/estado/{estado}")]
        public async Task<IActionResult> ObtenerAlertasPorEstado(string estado)
        {
            try
            {
                var alertas = await _context.Alertas
                    .Where(a => a.EstadoAlerta.ToLower() == estado.ToLower())
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

                return Ok(new { estadoFiltrado = estado, total = alertas.Count, alertas });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al filtrar alertas por estado", detail = ex.Message });
            }
        }

        // 3. Filtrar alertas por LABORATORIO
        [HttpGet("alertas/laboratorio/{laboratorioId}")]
        public async Task<IActionResult> ObtenerAlertasPorLaboratorio(int laboratorioId)
        {
            try
            {
                var alertas = await _context.Alertas
                    .Where(a => a.LaboratorioId == laboratorioId)
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

                return Ok(new { laboratorioId, total = alertas.Count, alertas });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al filtrar alertas por laboratorio", detail = ex.Message });
            }
        }

        // 4. Filtrar alertas por MÁQUINA
        [HttpGet("alertas/maquina/{maquinaId}")]
        public async Task<IActionResult> ObtenerAlertasPorMaquina(int maquinaId)
        {
            try
            {
                var alertas = await _context.Alertas
                    .Where(a => a.MaquinaId == maquinaId)
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

                return Ok(new { maquinaId, total = alertas.Count, alertas });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al filtrar alertas por máquina", detail = ex.Message });
            }
        }

        // 5. Filtrar alertas por RANGO DE FECHAS
        [HttpGet("alertas/rango-fechas")]
        public async Task<IActionResult> ObtenerAlertasPorRangoFechas(
            [FromQuery] DateTime fechaDesde,
            [FromQuery] DateTime fechaHasta)
        {
            try
            {
                if (fechaDesde > fechaHasta)
                    return BadRequest(new { message = "La fecha desde debe ser menor que la fecha hasta" });

                var alertas = await _context.Alertas
                    .Where(a => a.FechaCreacion >= fechaDesde && a.FechaCreacion <= fechaHasta)
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

                return Ok(new
                {
                    fechaDesde,
                    fechaHasta,
                    total = alertas.Count,
                    alertas
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al filtrar alertas por rango de fechas", detail = ex.Message });
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
            public string EstadoMaquinaDespues { get; set; } = "libre";
        }
    }
}
