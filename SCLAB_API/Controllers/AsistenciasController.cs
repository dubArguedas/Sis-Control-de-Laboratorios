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

        /*
     * CONTROLADOR DE ASISTENCIAS DE ESTUDIANTES
     * ==========================================
     * 
     * 1. POST /api/Asistencias/registrar
     *    - Registra la asistencia de un estudiante mediante código QR
     *    - Valida usuario estudiante, máquina, laboratorio y cronograma activo
     *    - Cambia el estado de la máquina a "ocupado"
     *    - Evita registros duplicados en el mismo horarios
     * 
     * 2. PUT /api/Asistencias/{id}/observacion
     *    - Actualiza la observación de una asistencia
     * 
     * 3. GET /api/Asistencias/{id}
     *    - Obtiene los detalles completos de una asistencia específica
     * 
     * 4. GET /api/Asistencias/usuario/{usuarioId}
     *    - Obtiene el historial de asistencias de un usuario específico
     * 
     * 5. GET /api/Asistencias/laboratorio/{laboratorioId}/activas
     *    - Lista las asistencias activas en un laboratorio
     * 
     * 6. PUT /api/Asistencias/{id}/finalizar
     *    - Registra la hora de salida de una asistencia
     */

        private readonly SisComputoDbContext _context;

        public AsistenciasController(SisComputoDbContext context)
        {
            _context = context;
        }

        // CLASE para el registro de asistencia
        public class RegistroAsistenciaDto
        {
            public int UsuarioId { get; set; }
            public int MaquinaId { get; set; }
            public int LaboratorioId { get; set; }
        }

        // CLASE para actualizar observación
        public class ActualizarObservacionDto
        {
            public string Observacion { get; set; } = string.Empty;
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // POST: api/Asistencias/registrar
        // Registrar asistencia de estudiante (por QR)
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpPost("registrar")]
        [AllowAnonymous] 
        public async Task<IActionResult> RegistrarAsistencia([FromBody] RegistroAsistenciaDto dto)
        {
            try
            {
                // 1. Validar que el usuario existe y es estudiante
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

                // 2. Validar que la máquina existe y está disponible
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

                // 3. Validar que el laboratorio existe
                var laboratorio = await _context.Laboratorios.FindAsync(dto.LaboratorioId);
                if (laboratorio == null)
                {
                    return NotFound(new { message = "El laboratorio no existe" });
                }

                // Validar que la máquina pertenece al laboratorio
                if (maquina.LaboratorioId != dto.LaboratorioId)
                {
                    return BadRequest(new { message = "La máquina no pertenece al laboratorio especificado" });
                }

                // 4. Obtener hora actual y día de la semana
                var horaActual = DateTime.Now;
                var diaSemana = ObtenerDiaSemanaEnEspanol(horaActual.DayOfWeek);
                var horaActualTimeSpan = horaActual.TimeOfDay;

                // DEBUG: Obtener todos los cronogramas del día para diagnóstico
                var cronogramasDelDia = await _context.CronogramaIntervals
                    .Where(c => c.LaboratorioId == dto.LaboratorioId
                        && c.DiaSemana.ToLower() == diaSemana.ToLower())
                    .Select(c => new
                    {
                        c.CronogramaId,
                        c.HoraInicio,
                        c.HoraFin,
                        c.Materia,
                        Coincide = c.HoraInicio <= horaActualTimeSpan && c.HoraFin > horaActualTimeSpan
                    })
                    .ToListAsync();

                // 5. Buscar el cronograma correspondiente
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
                        dia = diaSemana,
                        hora = horaActual.ToString("HH:mm:ss"),
                        debug = new
                        {
                            laboratorioId = dto.LaboratorioId,
                            horaActualTimeSpan = horaActualTimeSpan.ToString(@"hh\:mm\:ss"),
                            cronogramasDelDia = cronogramasDelDia.Select(c => new
                            {
                                c.CronogramaId,
                                HoraInicio = c.HoraInicio.ToString(@"hh\:mm\:ss"),
                                HoraFin = c.HoraFin.ToString(@"hh\:mm\:ss"),
                                c.Materia,
                                c.Coincide
                            })
                        }
                    });
                }

                // 6. Validar que el cronograma tenga materia asignada
                if (string.IsNullOrWhiteSpace(cronograma.Materia))
                {
                    return BadRequest(new 
                    { 
                        message = "El cronograma no tiene una materia asignada",
                        sugerencia = "Por favor, contacte al encargado para registrar un uso libre"
                    });
                }

                // 7. Verificar si ya existe un registro de asistencia en este horario
                var asistenciaExistente = await _context.Asistencias
                    .Include(a => a.Usuario)
                    .Include(a => a.Maquina)
                    .Include(a => a.Laboratorio)
                    .Include(a => a.Cronograma)
                    .Where(a => a.UsuarioId == dto.UsuarioId
                        && a.LaboratorioId == dto.LaboratorioId
                        && a.CronogramaId == cronograma.CronogramaId
                        && a.HoraIngreso.Date == horaActual.Date)
                    .FirstOrDefaultAsync();

                if (asistenciaExistente != null)
                {
                    // Devolver la asistencia existente con todos sus campos
                    return Ok(new
                    {
                        message = "Ya existe un registro de asistencia para este horario",
                    });
                }

                // 8. Crear el registro de asistencia
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
                    TipoDispositivo = "PC",
                    FechaRegistro = horaActual
                };

                _context.Asistencias.Add(nuevaAsistencia);

                // 9. Cambiar el estado de la máquina a ocupado
                maquina.Estado = "ocupado";

                await _context.SaveChangesAsync();

                // Devolver un objeto con los datos necesarios sin referencias circulares
                return Ok(new
                {
                    message = "Asistencia registrada exitosamente",
                    asistenciaId = nuevaAsistencia.AsistenciaId,
                    tipo = nuevaAsistencia.Tipo,
                    usuarioId = nuevaAsistencia.UsuarioId,
                    usuarioNombre = $"{usuario.Nombre} {usuario.ApellidoPaterno}",
                    maquinaId = nuevaAsistencia.MaquinaId,
                    maquinaCodigo = maquina.CodigoMaquina,
                    laboratorioId = nuevaAsistencia.LaboratorioId,
                    laboratorioCodigo = laboratorio.CodigoLaboratorio,
                    cronogramaId = nuevaAsistencia.CronogramaId,
                    materia = cronograma.Materia,
                    horaIngreso = nuevaAsistencia.HoraIngreso,
                    registroPor = nuevaAsistencia.RegistroPor,
                    tipoDispositivo = nuevaAsistencia.TipoDispositivo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }


        //-----------------------------------------------------------------------------------------------------------------------------
        // POST: api/Asistencias/registrar/uso_libre
        // Registrar asistencia de estudiante por interfaz de administrador
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpPost("registrar/uso_libre")]
        [AllowAnonymous]
        public async Task<IActionResult> RegistrarAsistenciaHoraLibre([FromBody] RegistroAsistenciaDto dto)
        {
            try
            {
                // 1. Validar que el usuario existe y es estudiante
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

                // 2. Validar que la máquina existe y está disponible
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

                // 3. Validar que el laboratorio existe
                var laboratorio = await _context.Laboratorios.FindAsync(dto.LaboratorioId);
                if (laboratorio == null)
                {
                    return NotFound(new { message = "El laboratorio no existe" });
                }

                // Validar que la máquina pertenece al laboratorio
                if (maquina.LaboratorioId != dto.LaboratorioId)
                {
                    return BadRequest(new { message = "La máquina no pertenece al laboratorio especificado" });
                }

                // 4. Obtener hora actual y día de la semana
                var horaActual = DateTime.Now;
                var diaSemana = ObtenerDiaSemanaEnEspanol(horaActual.DayOfWeek);
                var horaActualTimeSpan = horaActual.TimeOfDay;

                // DEBUG: Obtener todos los cronogramas del día para diagnóstico
                var cronogramasDelDia = await _context.CronogramaIntervals
                    .Where(c => c.LaboratorioId == dto.LaboratorioId
                        && c.DiaSemana.ToLower() == diaSemana.ToLower())
                    .Select(c => new
                    {
                        c.CronogramaId,
                        c.HoraInicio,
                        c.HoraFin,
                        c.Materia,
                        Coincide = c.HoraInicio <= horaActualTimeSpan && c.HoraFin > horaActualTimeSpan
                    })
                    .ToListAsync();

                // 5. Buscar el cronograma correspondiente
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
                        dia = diaSemana,
                        hora = horaActual.ToString("HH:mm:ss"),
                        debug = new
                        {
                            laboratorioId = dto.LaboratorioId,
                            horaActualTimeSpan = horaActualTimeSpan.ToString(@"hh\:mm\:ss"),
                            cronogramasDelDia = cronogramasDelDia.Select(c => new
                            {
                                c.CronogramaId,
                                HoraInicio = c.HoraInicio.ToString(@"hh\:mm\:ss"),
                                HoraFin = c.HoraFin.ToString(@"hh\:mm\:ss"),
                                c.Materia,
                                c.Coincide
                            })
                        }
                    });
                }

                // 6. Validar que el cronograma tenga materia asignada
                if (string.IsNullOrWhiteSpace(cronograma.Materia))
                {
                    return BadRequest(new
                    {
                        message = "El cronograma no tiene una materia asignada",
                        sugerencia = "Por favor, contacte al encargado para registrar un uso libre"
                    });
                }

                // 7. Verificar si ya existe un registro de asistencia en este horario
                var asistenciaExistente = await _context.Asistencias
                    .Include(a => a.Usuario)
                    .Include(a => a.Maquina)
                    .Include(a => a.Laboratorio)
                    .Include(a => a.Cronograma)
                    .Where(a => a.UsuarioId == dto.UsuarioId
                        && a.LaboratorioId == dto.LaboratorioId
                        && a.CronogramaId == cronograma.CronogramaId
                        && a.HoraIngreso.Date == horaActual.Date)
                    .FirstOrDefaultAsync();

                if (asistenciaExistente != null)
                {
                    // Devolver la asistencia existente con todos sus campos
                    return Ok(new
                    {
                        message = "Ya existe un registro de asistencia para este horario",
                    });
                }

                // 8. Crear el registro de asistencia
                var nuevaAsistencia = new Asistencia
                {
                    Tipo = "uso_libre",
                    UsuarioId = dto.UsuarioId,
                    MaquinaId = dto.MaquinaId,
                    LaboratorioId = dto.LaboratorioId,
                    CronogramaId = cronograma.CronogramaId,
                    RegistroPor = "administrador",
                    HoraIngreso = horaActual,
                    HoraSalida = null,
                    RolRegistro = "estudiante",
                    Observacion = null,
                    TipoDispositivo = "PC",
                    FechaRegistro = horaActual
                };

                _context.Asistencias.Add(nuevaAsistencia);

                // 9. Cambiar el estado de la máquina a ocupado
                maquina.Estado = "ocupado";

                await _context.SaveChangesAsync();

                // Devolver un objeto con los datos necesarios sin referencias circulares
                return Ok(new
                {
                    message = "Asistencia registrada exitosamente",
                    asistenciaId = nuevaAsistencia.AsistenciaId,
                    tipo = nuevaAsistencia.Tipo,
                    usuarioId = nuevaAsistencia.UsuarioId,
                    usuarioNombre = $"{usuario.Nombre} {usuario.ApellidoPaterno}",
                    maquinaId = nuevaAsistencia.MaquinaId,
                    maquinaCodigo = maquina.CodigoMaquina,
                    laboratorioId = nuevaAsistencia.LaboratorioId,
                    laboratorioCodigo = laboratorio.CodigoLaboratorio,
                    cronogramaId = nuevaAsistencia.CronogramaId,
                    materia = cronograma.Materia,
                    horaIngreso = nuevaAsistencia.HoraIngreso,
                    registroPor = nuevaAsistencia.RegistroPor,
                    tipoDispositivo = nuevaAsistencia.TipoDispositivo
                });
            }
            catch (Exception ex)
            {
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

                // Actualizar la observación
                asistencia.Observacion = dto.Observacion;

                // Cambiar el estado de la máquina a mantenimiento
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
                        Cronograma = new
                        {
                            a.Cronograma.Materia,
                            a.Cronograma.DiaSemana,
                            HoraInicio = a.Cronograma.HoraInicio.ToString(@"hh\:mm"),
                            HoraFin = a.Cronograma.HoraFin.ToString(@"hh\:mm")
                        },
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
                        Materia = a.Cronograma.Materia,
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
                        Materia = a.Cronograma.Materia,
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

                // Registrar hora de salida
                asistencia.HoraSalida = DateTime.Now;

                // Cambiar el estado de la máquina a disponible (solo si no está en mantenimiento)
                if (asistencia.Maquina != null && asistencia.Maquina.Estado.ToLower() != "mantenimiento")
                {
                    asistencia.Maquina.Estado = "disponible";
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