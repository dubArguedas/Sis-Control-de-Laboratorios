using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCLAB_API.Data;
using SCLAB_API.Models;

namespace SCLAB_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AsistenciasController : ControllerBase
    {
        private readonly SisComputoDbContext _context;

        public AsistenciasController(SisComputoDbContext context)
        {
            _context = context;
        }

        public class RegistroAsistenciaDto
        {
            public int UsuarioId { get; set; }
            public int MaquinaId { get; set; }
            public int LaboratorioId { get; set; }
            public string TipoDisp { get; set; } = "PC";
        }

        public class ActualizarObservacionDto
        {
            public string Observacion { get; set; } = string.Empty;
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // POST: api/Asistencias/registrar
        // Registrar asistencia de estudiante (por QR) - TIPO: programada
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpPost("registrar")]
        [AllowAnonymous]
        public async Task<IActionResult> RegistrarAsistencia([FromBody] RegistroAsistenciaDto dto)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(dto.UsuarioId);
                if (usuario == null)
                {
                    return NotFound(new { message = "El usuario no existe" });
                }

                if (usuario.Rol.ToLower() != "estudiante")
                {
                    return BadRequest(new { message = "El usuario debe ser de tipo estudiante" });
                }

                if (usuario.Estado.ToLower() != "activo")
                {
                    return BadRequest(new { message = "El usuario no está activo" });
                }

                var maquina = await _context.Maquinas
                    .Include(m => m.Laboratorio)
                    .FirstOrDefaultAsync(m => m.MaquinaId == dto.MaquinaId);

                if (maquina == null)
                {
                    return NotFound(new { message = "La máquina no existe" });
                }

                if (maquina.Estado.ToLower() == "mantenimiento")
                {
                    return BadRequest(new { message = "La máquina está en mantenimiento" });
                }
                if (maquina.Estado.ToLower() == "ocupado")
                {
                    return BadRequest(new { message = "La máquina está en uso" });
                }

                var laboratorio = await _context.Laboratorios.FindAsync(dto.LaboratorioId);
                if (laboratorio == null)
                {
                    return NotFound(new { message = "El laboratorio no existe" });
                }

                if (maquina.LaboratorioId != dto.LaboratorioId)
                {
                    return BadRequest(new { message = "La máquina no pertenece al laboratorio especificado" });
                }

                var horaActual = DateTime.Now;
                var diaSemana = ObtenerDiaSemanaEnEspanol(horaActual.DayOfWeek);
                var horaActualTimeSpan = horaActual.TimeOfDay;

                var cronograma = await _context.CronogramaIntervals
                    .Where(c => c.LaboratorioId == dto.LaboratorioId
                        && c.DiaSemana.ToLower() == diaSemana.ToLower()
                        && c.HoraInicio <= horaActualTimeSpan
                        && c.HoraFin > horaActualTimeSpan)
                    .FirstOrDefaultAsync();

                if (cronograma == null)
                {
                    return BadRequest(new
                    {
                        message = "No hay un horario programado para este laboratorio en este momento",
                        sugerencia = "Contacte al encargado para registrar un uso libre"
                    });
                }

                if (string.IsNullOrWhiteSpace(cronograma.Materia))
                {
                    return BadRequest(new
                    {
                        message = "El cronograma no tiene una materia asignada",
                        sugerencia = "Por favor, contacte al encargado para registrar un uso libre"
                    });
                }

                var asistenciaExistente = await _context.Asistencias
                    .Where(a => a.UsuarioId == dto.UsuarioId
                        && a.LaboratorioId == dto.LaboratorioId
                        && a.CronogramaId == cronograma.CronogramaId
                        && a.HoraIngreso.Date == horaActual.Date)
                    .FirstOrDefaultAsync();

                if (asistenciaExistente != null)
                {
                    return Ok(new
                    {
                        message = "Ya existe un registro de asistencia para este horario",
                        asistenciaId = asistenciaExistente.AsistenciaId
                    });
                }

                var nuevaAsistencia = new Asistencia
                {
                    Tipo = "programada",
                    UsuarioId = dto.UsuarioId,
                    MaquinaId = dto.MaquinaId,
                    LaboratorioId = dto.LaboratorioId,
                    CronogramaId = cronograma.CronogramaId,
                    RegistroPor = "qr",
                    HoraIngreso = horaActual,
                    HoraSalida = null,
                    RolRegistro = "estudiante",
                    Observacion = null,
                    TipoDispositivo = dto.TipoDisp,
                    FechaRegistro = horaActual
                };

                _context.Asistencias.Add(nuevaAsistencia);
                maquina.Estado = "ocupado";
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Asistencia registrada exitosamente",
                    asistenciaId = nuevaAsistencia.AsistenciaId,
                    tipo = nuevaAsistencia.Tipo,
                    materia = cronograma.Materia
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // POST: api/Asistencias/registrar/uso_libre
        // Registrar asistencia de estudiante por interfaz de administrador - TIPO: uso_libre
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpPost("registrar/uso_libre")]
        [AllowAnonymous]
        public async Task<IActionResult> RegistrarAsistenciaUsoLibre([FromBody] RegistroAsistenciaDto dto)
        {
            try
            {
                Console.WriteLine($"[UsoLibre] Recibido: UsuarioId={dto.UsuarioId}, MaquinaId={dto.MaquinaId}, LabId={dto.LaboratorioId}");

                var usuario = await _context.Usuarios.FindAsync(dto.UsuarioId);
                if (usuario == null)
                {
                    return NotFound(new { message = "El usuario no existe" });
                }

                if (usuario.Rol.ToLower() != "estudiante")
                {
                    return BadRequest(new { message = "El usuario debe ser de tipo estudiante" });
                }

                if (usuario.Estado.ToLower() != "activo")
                {
                    return BadRequest(new { message = "El usuario no está activo" });
                }

                var maquina = await _context.Maquinas
                    .Include(m => m.Laboratorio)
                    .FirstOrDefaultAsync(m => m.MaquinaId == dto.MaquinaId);

                if (maquina == null)
                {
                    return NotFound(new { message = "La máquina no existe" });
                }

                if (maquina.Estado.ToLower() == "mantenimiento")
                {
                    return BadRequest(new { message = "La máquina está en mantenimiento" });
                }

                if (maquina.Estado.ToLower() == "ocupado")
                {
                    return BadRequest(new { message = "La máquina ya está ocupada" });
                }

                var laboratorio = await _context.Laboratorios.FindAsync(dto.LaboratorioId);
                if (laboratorio == null)
                {
                    return NotFound(new { message = "El laboratorio no existe" });
                }

                if (maquina.LaboratorioId != dto.LaboratorioId)
                {
                    return BadRequest(new { message = "La máquina no pertenece al laboratorio especificado" });
                }

                var horaActual = DateTime.Now;
                var diaSemana = ObtenerDiaSemanaEnEspanol(horaActual.DayOfWeek);
                var horaActualTimeSpan = horaActual.TimeOfDay;

                // Buscar cronograma actual (puede o no tener materia)
                var cronograma = await _context.CronogramaIntervals
                    .Where(c => c.LaboratorioId == dto.LaboratorioId
                        && c.DiaSemana.ToLower() == diaSemana.ToLower()
                        && c.HoraInicio <= horaActualTimeSpan
                        && c.HoraFin >= horaActualTimeSpan)
                    .FirstOrDefaultAsync();

                // Para uso libre, el cronograma es opcional
                int? cronogramaId = cronograma?.CronogramaId;

                // Crear registro de asistencia de tipo "uso_libre"
                var nuevaAsistencia = new Asistencia
                {
                    Tipo = "uso_libre",
                    UsuarioId = dto.UsuarioId,
                    MaquinaId = dto.MaquinaId,
                    LaboratorioId = dto.LaboratorioId,
                    CronogramaId = cronogramaId,
                    RegistroPor = "administrador",
                    HoraIngreso = horaActual,
                    HoraSalida = null,
                    RolRegistro = "estudiante",
                    Observacion = null,
                    TipoDispositivo = "PC",
                    FechaRegistro = horaActual
                };

                _context.Asistencias.Add(nuevaAsistencia);

                // Actualizar estado de máquina a ocupado
                maquina.Estado = "ocupado";
                _context.Maquinas.Update(maquina);

                await _context.SaveChangesAsync();

                Console.WriteLine($"[UsoLibre] ✅ Asistencia creada ID={nuevaAsistencia.AsistenciaId}, Máquina ahora: {maquina.Estado}");

                return Ok(new
                {
                    message = "Uso libre registrado exitosamente",
                    asistenciaId = nuevaAsistencia.AsistenciaId,
                    tipo = nuevaAsistencia.Tipo,
                    usuarioNombre = $"{usuario.Nombre} {usuario.ApellidoPaterno}",
                    maquinaCodigo = maquina.CodigoMaquina,
                    laboratorioCodigo = laboratorio.CodigoLaboratorio
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UsoLibre] ❌ Error: {ex.Message}");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // PUT: api/Asistencias/{id}/observacion
        // Actualizar observación de una asistencia
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpPut("{id}/observacion")]
        public async Task<IActionResult> ActualizarObservacion(int id, [FromBody] ActualizarObservacionDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Observacion))
                {
                    return BadRequest(new { message = "La observación no puede estar vacía" });
                }

                var asistencia = await _context.Asistencias
                    .Include(a => a.Maquina)
                    .FirstOrDefaultAsync(a => a.AsistenciaId == id);

                if (asistencia == null)
                {
                    return NotFound(new { message = "La asistencia no existe" });
                }

                asistencia.Observacion = dto.Observacion;

                if (asistencia.Maquina != null)
                {
                    asistencia.Maquina.Estado = "mantenimiento";
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Observación actualizada y máquina en mantenimiento",
                    asistenciaId = asistencia.AsistenciaId,
                    observacion = asistencia.Observacion,
                    maquinaEstado = asistencia.Maquina?.Estado
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // GET: api/Asistencias/{id}
        // Obtener una asistencia específica
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerAsistencia(int id)
        {
            try
            {
                var asistencia = await _context.Asistencias
                    .Include(a => a.Usuario)
                    .Include(a => a.Maquina)
                    .Include(a => a.Laboratorio)
                    .Include(a => a.Cronograma)
                    .Where(a => a.AsistenciaId == id)
                    .Select(a => new
                    {
                        a.AsistenciaId,
                        a.Tipo,
                        a.UsuarioId,
                        Usuario = new
                        {
                            a.Usuario.Nombre,
                            a.Usuario.ApellidoPaterno,
                            a.Usuario.ApellidoMaterno,
                            a.Usuario.CorreoInstitucional,
                            a.Usuario.Rol
                        },
                        a.MaquinaId,
                        Maquina = new
                        {
                            a.Maquina.CodigoMaquina,
                            a.Maquina.Estado,
                            a.Maquina.DescripcionHardware
                        },
                        a.LaboratorioId,
                        Laboratorio = new
                        {
                            a.Laboratorio.CodigoLaboratorio,
                            a.Laboratorio.Ubicacion,
                            a.Laboratorio.Estado
                        },
                        a.CronogramaId,
                        Cronograma = a.Cronograma != null ? new
                        {
                            a.Cronograma.Materia,
                            a.Cronograma.DiaSemana,
                            HoraInicio = a.Cronograma.HoraInicio.ToString(@"hh\:mm"),
                            HoraFin = a.Cronograma.HoraFin.ToString(@"hh\:mm")
                        } : null,
                        a.RegistroPor,
                        a.HoraIngreso,
                        a.HoraSalida,
                        a.DuracionUso,
                        a.RolRegistro,
                        a.Observacion,
                        a.TipoDispositivo,
                        a.FechaRegistro
                    })
                    .FirstOrDefaultAsync();

                if (asistencia == null)
                {
                    return NotFound(new { message = "Asistencia no encontrada" });
                }

                return Ok(asistencia);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // GET: api/Asistencias/usuario/{usuarioId}
        // Obtener asistencias de un usuario
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpGet("usuario/{usuarioId}")]
        public async Task<IActionResult> ObtenerAsistenciasPorUsuario(int usuarioId)
        {
            try
            {
                var asistencias = await _context.Asistencias
                    .Include(a => a.Usuario)
                    .Include(a => a.Maquina)
                    .Include(a => a.Laboratorio)
                    .Include(a => a.Cronograma)
                    .Where(a => a.UsuarioId == usuarioId)
                    .OrderByDescending(a => a.FechaRegistro)
                    .Select(a => new
                    {
                        a.AsistenciaId,
                        a.Tipo,
                        a.MaquinaId,
                        MaquinaCodigo = a.Maquina.CodigoMaquina,
                        a.LaboratorioId,
                        LaboratorioCodigo = a.Laboratorio.CodigoLaboratorio,
                        Materia = a.Cronograma != null ? a.Cronograma.Materia : null,
                        a.HoraIngreso,
                        a.HoraSalida,
                        a.DuracionUso,
                        a.Observacion,
                        a.FechaRegistro
                    })
                    .ToListAsync();

                return Ok(asistencias);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // GET: api/Asistencias/laboratorio/{laboratorioId}/activas
        // Obtener asistencias activas de un laboratorio (sin hora de salida)
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpGet("laboratorio/{laboratorioId}/activas")]
        public async Task<IActionResult> ObtenerAsistenciasActivas(int laboratorioId)
        {
            try
            {
                var asistenciasActivas = await _context.Asistencias
                    .Include(a => a.Usuario)
                    .Include(a => a.Maquina)
                    .Include(a => a.Cronograma)
                    .Where(a => a.LaboratorioId == laboratorioId && a.HoraSalida == null)
                    .OrderBy(a => a.HoraIngreso)
                    .Select(a => new
                    {
                        a.AsistenciaId,
                        a.UsuarioId,
                        UsuarioNombre = $"{a.Usuario.Nombre} {a.Usuario.ApellidoPaterno}",
                        a.MaquinaId,
                        MaquinaCodigo = a.Maquina.CodigoMaquina,
                        Materia = a.Cronograma != null ? a.Cronograma.Materia : "Uso Libre",
                        a.HoraIngreso,
                        TiempoTranscurrido = DateTime.Now - a.HoraIngreso,
                        a.Observacion
                    })
                    .ToListAsync();

                return Ok(new
                {
                    laboratorioId,
                    totalActivas = asistenciasActivas.Count,
                    asistencias = asistenciasActivas
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // PUT: api/Asistencias/{id}/finalizar
        // Finalizar una asistencia (registrar hora de salida)
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpPut("{id}/finalizar")]
        public async Task<IActionResult> FinalizarAsistencia(int id)
        {
            try
            {
                var asistencia = await _context.Asistencias
                    .Include(a => a.Maquina)
                    .FirstOrDefaultAsync(a => a.AsistenciaId == id);

                if (asistencia == null)
                {
                    return NotFound(new { message = "La asistencia no existe" });
                }

                if (asistencia.HoraSalida.HasValue)
                {
                    return BadRequest(new { message = "Esta asistencia ya ha sido finalizada" });
                }

                asistencia.HoraSalida = DateTime.Now;

                if (asistencia.Maquina != null && asistencia.Maquina.Estado.ToLower() != "mantenimiento")
                {
                    asistencia.Maquina.Estado = "libre";
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Asistencia finalizada exitosamente",
                    asistenciaId = asistencia.AsistenciaId,
                    horaIngreso = asistencia.HoraIngreso,
                    horaSalida = asistencia.HoraSalida,
                    duracionUso = asistencia.DuracionUso,
                    maquinaEstado = asistencia.Maquina?.Estado
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // GET: api/Asistencias/maquina/{maquinaId}/ultima-observacion
        // Obtener la última observación de una máquina (para mantenimiento)
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpGet("maquina/{maquinaId}/ultima-observacion")]
        public async Task<IActionResult> ObtenerUltimaObservacionMaquina(int maquinaId)
        {
            try
            {
                var ultimaAsistencia = await _context.Asistencias
                    .Where(a => a.MaquinaId == maquinaId && !string.IsNullOrEmpty(a.Observacion))
                    .OrderByDescending(a => a.FechaRegistro)
                    .Select(a => new
                    {
                        a.AsistenciaId,
                        a.Observacion,
                        a.FechaRegistro,
                        a.HoraIngreso
                    })
                    .FirstOrDefaultAsync();

                if (ultimaAsistencia == null)
                {
                    return Ok(new
                    {
                        observacion = "Sin observación registrada",
                        fechaRegistro = (DateTime?)null
                    });
                }

                return Ok(new
                {
                    observacion = ultimaAsistencia.Observacion,
                    fechaRegistro = ultimaAsistencia.FechaRegistro
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // Método auxiliar para obtener el día de la semana en español
        //-----------------------------------------------------------------------------------------------------------------------------
        private string ObtenerDiaSemanaEnEspanol(DayOfWeek dia)
        {
            return dia switch
            {
                DayOfWeek.Monday => "lunes",
                DayOfWeek.Tuesday => "martes",
                DayOfWeek.Wednesday => "miercoles",
                DayOfWeek.Thursday => "jueves",
                DayOfWeek.Friday => "viernes",
                DayOfWeek.Saturday => "sabado",
                DayOfWeek.Sunday => "domingo",
                _ => ""
            };
        }
    }
}