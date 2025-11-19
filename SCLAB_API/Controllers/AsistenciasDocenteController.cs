using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCLAB_API.Data;
using SCLAB_API.Models;
using System.Drawing.Drawing2D;

namespace SCLAB_API.Controllers
{

    /*
     * CONTROLADOR DE ASISTENCIAS DE DOCENTES
     * ======================================
     * 1. POST /api/AsistenciasDocente/registrar
     *    - Registra la asistencia de un docente mediante código QR
     *    - Valida usuario docente, máquina, laboratorio y cronograma activo
     *    - Cambia el estado de máquina y laboratorio a "ocupado"
     *    - Evita registros duplicados en el mismo horario
     * 
     * 2. PUT /api/AsistenciasDocente/{id}/observacion
     *    - Actualiza la observación de una asistencia de docente
     * 
     * 3. GET /api/AsistenciasDocente/{id}
     *    - Obtiene los detalles completos de una asistencia específica de docente
     * 
     * 4. GET /api/AsistenciasDocente/docente/{usuarioId}
     *    - Obtiene el historial de asistencias de un docente específico
     * 
     * 5. GET /api/AsistenciasDocente/laboratorio/{laboratorioId}/activas
     *    - Lista las asistencias activas de docentes en un laboratorio
     * 
     * 6. PUT /api/AsistenciasDocente/{id}/finalizar
     *    - Registra la hora de salida de una asistencia de docente
     *    - Cambia el estado de la máquina a "disponible" (si no está en mantenimiento)
     *    - Cambia el estado del laboratorio a "libre" si no hay más asistencias activas
     * 
     * 7. GET /api/AsistenciasDocente/materia/{materia}
     *    - Busca asistencias de docentes por nombre de materia
     * 
     * 8. GET /api/AsistenciasDocente/materia/busqueda/{materia}
     *    - Busca asistencias de ESTUDIANTES por nombre de materia
     * 
     * 9. GET /api/AsistenciasDocente/busqueda/horario/{diaSemana}/{horaentrada}/{horasalida}
     *    - Busca asistencias de ESTUDIANTES por día y rango horario
     * 
     * 10. GET /api/AsistenciasDocente/busqueda/general
     *     - Obtiene todas las asistencias del sistema (docentes y estudiantes)
     */
    [Route("api/[controller]")]
    [ApiController]
    public class AsistenciasDocenteController : ControllerBase
    {
        private readonly SisComputoDbContext _context;

        public AsistenciasDocenteController(SisComputoDbContext context)
        {
            _context = context;
        }

        // DTO para el registro de asistencia de docente
        public class RegistroAsistenciaDocenteDto
        {
            public int UsuarioId { get; set; }
            public int MaquinaId { get; set; }
            public int LaboratorioId { get; set; }
        }

        // DTO para actualizar observación
        public class ActualizarObservacionDocentesDto
        {
            public string Observacion { get; set; } = string.Empty;
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // POST: api/AsistenciasDocente/registrar
        // Registrar asistencia de docente (por QR)
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpPost("registrar")]
        [AllowAnonymous]
        public async Task<IActionResult> RegistrarAsistenciaDocente([FromBody] RegistroAsistenciaDocenteDto dto)
        {
            try
            {
                // 1. Validar que el usuario existe y es docente
                var usuario = await _context.Usuarios.FindAsync(dto.UsuarioId);
                if (usuario == null)
                {
                    return NotFound(new { message = "El usuario no existe" });
                }

                if (usuario.Rol.ToLower() != "docente")
                {
                    return BadRequest(new { message = "El usuario debe ser de tipo docente" });
                }

                if (usuario.Estado.ToLower() != "activo")
                {
                    return BadRequest(new { message = "El usuario no está activo" });
                }

                // 2. Validar que la máquina existe
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

                // 5. Buscar el cronograma correspondiente
                var cronograma = await _context.CronogramaIntervals
                    .Where(c => c.LaboratorioId == dto.LaboratorioId
                        && c.DiaSemana.ToLower() == diaSemana.ToLower()
                        && c.HoraInicio <= horaActualTimeSpan
                        && c.HoraFin >= horaActualTimeSpan)
                    .FirstOrDefaultAsync();

                if (cronograma == null)
                {
                    return BadRequest(new 
                    { 
                        message = "No hay un horario programado para este laboratorio en este momento",
                        sugerencia = "Por favor, contacte al encargado para registrar un uso libre",
                    });
                }

                // 6. Validar que el cronograma tenga materia asignada
                if (string.IsNullOrWhiteSpace(cronograma.Materia))
                {
                    return BadRequest(new 
                    { 
                        message = "El cronograma no tiene una materia asignada",
                        sugerencia = "Por favor, contacte al encargado para registrar un uso libre del laboratorio"
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
                    RolRegistro = "docente",
                    Observacion = null,
                    TipoDispositivo = "PC",
                    FechaRegistro = horaActual
                };

                _context.Asistencias.Add(nuevaAsistencia);

                // 9. Cambiar el estado de la máquina a ocupado
                maquina.Estado = "ocupado";

                // 10. Cambiar el estado del laboratorio a ocupado
                laboratorio.Estado = "ocupado";

                await _context.SaveChangesAsync();

                // Devolver un objeto con los datos necesarios sin referencias circulares
                return Ok(new
                {
                    message = "Asistencia de docente registrada exitosamente",
                    asistenciaId = nuevaAsistencia.AsistenciaId,
                    tipo = nuevaAsistencia.Tipo,
                    usuarioId = nuevaAsistencia.UsuarioId,
                    docenteNombre = $"{usuario.Nombre} {usuario.ApellidoPaterno}",
                    maquinaId = nuevaAsistencia.MaquinaId,
                    maquinaCodigo = maquina.CodigoMaquina,
                    laboratorioId = nuevaAsistencia.LaboratorioId,
                    laboratorioCodigo = laboratorio.CodigoLaboratorio,
                    laboratorioEstado = laboratorio.Estado,
                    cronogramaId = nuevaAsistencia.CronogramaId,
                    materia = cronograma.Materia,
                    horaIngreso = nuevaAsistencia.HoraIngreso,
                    registroPor = nuevaAsistencia.RegistroPor,
                    rolRegistro = nuevaAsistencia.RolRegistro,
                    tipoDispositivo = nuevaAsistencia.TipoDispositivo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // PUT: api/AsistenciasDocente/{id}/observacion
        // Actualizar observación de una asistencia de docente
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpPut("{id}/observacion")]
        public async Task<IActionResult> ActualizarObservacionDocente(int id, [FromBody] ActualizarObservacionDocentesDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Observacion))
                {
                    return BadRequest(new { message = "La observación no puede estar vacía" });
                }

                var asistencia = await _context.Asistencias
                    .Include(a => a.Maquina)
                    .Include(a => a.Usuario)
                    .FirstOrDefaultAsync(a => a.AsistenciaId == id);

                if (asistencia == null)
                {
                    return NotFound(new { message = "La asistencia no existe" });
                }

                // Validar que sea una asistencia de docente
                if (asistencia.RolRegistro.ToLower() != "docente")
                {
                    return BadRequest(new { message = "Esta asistencia no corresponde a un docente" });
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
                    maquinaEstado = asistencia.Maquina?.Estado,
                    docente = $"{asistencia.Usuario?.Nombre} {asistencia.Usuario?.ApellidoPaterno}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // GET: api/AsistenciasDocente/{id}
        // Obtener una asistencia específica de docente
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerAsistenciaDocente(int id)
        {
            try
            {
                var asistencia = await _context.Asistencias
                    .Include(a => a.Usuario)
                    .Include(a => a.Maquina)
                    .Include(a => a.Laboratorio)
                    .Include(a => a.Cronograma)
                    .Where(a => a.AsistenciaId == id && a.RolRegistro.ToLower() == "docente")
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
                        Materia = a.Cronograma.Materia,
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
                    return NotFound(new { message = "Asistencia de docente no encontrada" });
                }

                return Ok(asistencia);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // GET: api/AsistenciasDocente/docente/{usuarioId}
        // Obtener asistencias de un docente
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpGet("docente/{usuarioId}")]
        public async Task<IActionResult> ObtenerAsistenciasPorDocente(int usuarioId)
        {
            try
            {
                // Validar que el usuario es docente
                var usuario = await _context.Usuarios.FindAsync(usuarioId);
                if (usuario == null)
                {
                    return NotFound(new { message = "El usuario no existe" });
                }

                if (usuario.Rol.ToLower() != "docente")
                {
                    return BadRequest(new { message = "El usuario no es docente" });
                }

                var asistencias = await _context.Asistencias
                    .Include(a => a.Usuario)
                    .Include(a => a.Maquina)
                    .Include(a => a.Laboratorio)
                    .Include(a => a.Cronograma)
                    .Where(a => a.UsuarioId == usuarioId && a.RolRegistro.ToLower() == "docente")
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

                return Ok(new
                {
                    docenteId = usuarioId,
                    nombreDocente = $"{usuario.Nombre} {usuario.ApellidoPaterno}",
                    totalAsistencias = asistencias.Count,
                    asistencias
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // GET: api/AsistenciasDocente/laboratorio/{laboratorioId}/activas
        // Obtener asistencias activas de docentes en un laboratorio
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpGet("laboratorio/{laboratorioId}/activas")]
        public async Task<IActionResult> ObtenerAsistenciasDocentesActivas(int laboratorioId)
        {
            try
            {
                var asistenciasActivas = await _context.Asistencias
                    .Include(a => a.Usuario)
                    .Include(a => a.Maquina)
                    .Include(a => a.Cronograma)
                    .Where(a => a.LaboratorioId == laboratorioId 
                        && a.HoraSalida == null 
                        && a.RolRegistro.ToLower() == "docente")
                    .OrderBy(a => a.HoraIngreso)
                    .Select(a => new
                    {
                        a.AsistenciaId,
                        a.UsuarioId,
                        DocenteNombre = $"{a.Usuario.Nombre} {a.Usuario.ApellidoPaterno}",
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
                    totalActivasDocentes = asistenciasActivas.Count,
                    asistencias = asistenciasActivas
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // PUT: api/AsistenciasDocente/{id}/finalizar
        // Finalizar una asistencia de docente (registrar hora de salida)
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpPut("{id}/finalizar")]
        public async Task<IActionResult> FinalizarAsistenciaDocente(int id)
        {
            try
            {
                var asistencia = await _context.Asistencias
                    .Include(a => a.Maquina)
                    .Include(a => a.Laboratorio)
                    .Include(a => a.Usuario)
                    .FirstOrDefaultAsync(a => a.AsistenciaId == id);

                if (asistencia == null)
                {
                    return NotFound(new { message = "La asistencia no existe" });
                }

                if (asistencia.RolRegistro.ToLower() != "docente")
                {
                    return BadRequest(new { message = "Esta asistencia no corresponde a un docente" });
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

                // Verificar si hay más asistencias activas en el laboratorio
                var asistenciasActivasEnLab = await _context.Asistencias
                    .Where(a => a.LaboratorioId == asistencia.LaboratorioId 
                        && a.HoraSalida == null 
                        && a.AsistenciaId != id)
                    .CountAsync();

                // Si no hay más asistencias activas, cambiar el estado del laboratorio a libre
                if (asistenciasActivasEnLab == 0 && asistencia.Laboratorio != null)
                {
                    asistencia.Laboratorio.Estado = "libre";
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Asistencia de docente finalizada exitosamente",
                    asistenciaId = asistencia.AsistenciaId,
                    docente = $"{asistencia.Usuario?.Nombre} {asistencia.Usuario?.ApellidoPaterno}",
                    horaIngreso = asistencia.HoraIngreso,
                    horaSalida = asistencia.HoraSalida,
                    duracionUso = asistencia.DuracionUso,
                    maquinaEstado = asistencia.Maquina?.Estado,
                    laboratorioEstado = asistencia.Laboratorio?.Estado
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // GET: api/AsistenciasDocente/materia/{materia}
        // Obtener asistencias por materia
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpGet("materia/{materia}")]
        public async Task<IActionResult> ObtenerAsistenciasPorMateria(string materia)
        {
            try
            {
                var asistencias = await _context.Asistencias
                    .Include(a => a.Usuario)
                    .Include(a => a.Maquina)
                    .Include(a => a.Laboratorio)
                    .Include(a => a.Cronograma)
                    .Where(a => a.RolRegistro.ToLower() == "docente" 
                        && a.Cronograma != null 
                        && a.Cronograma.Materia != null
                        && a.Cronograma.Materia.ToLower().Contains(materia.ToLower()))
                    .OrderByDescending(a => a.FechaRegistro)
                    .Select(a => new
                    {
                        a.AsistenciaId,
                        DocenteNombre = $"{a.Usuario.Nombre} {a.Usuario.ApellidoPaterno}",
                        a.Usuario.CorreoInstitucional,
                        LaboratorioCodigo = a.Laboratorio.CodigoLaboratorio,
                        Materia = a.Cronograma.Materia,
                        a.HoraIngreso,
                        a.HoraSalida,
                        a.DuracionUso,
                        a.FechaRegistro
                    })
                    .ToListAsync();

                return Ok(new
                {
                    materiaBuscada = materia,
                    totalAsistencias = asistencias.Count,
                    asistencias
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // GET: api/AsistenciasDocente/materia/busqueda/{materia}
        // Obtener asistencias de estudiantes por materia
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpGet("materia/busqueda/{materia}")]
        public async Task<IActionResult> ObtenerAsistenciasEstudiantesporMateria(string materia)
        {
            try
            {
                var asistencias = await _context.Asistencias
                    .Include(a => a.Usuario)
                    .Include(a => a.Maquina)
                    .Include(a => a.Laboratorio)
                    .Include(a => a.Cronograma)
                    .OrderByDescending(a => a.FechaRegistro)
                    .Where(a => a.RolRegistro.ToLower() == "estudiante"
                        && a.Cronograma != null
                        && a.Cronograma.Materia != null
                        && a.Cronograma.Materia.ToLower().Contains(materia.ToLower()))
                    .Select(a => new
                    {
                        a.AsistenciaId,
                        EstudianteNombre = $"{a.Usuario.Nombre} {a.Usuario.ApellidoPaterno}",
                        a.Usuario.CorreoInstitucional,
                        LaboratorioCodigo = a.Laboratorio.CodigoLaboratorio,
                        Materia = a.Cronograma.Materia,
                        a.HoraIngreso,
                        a.HoraSalida,
                        a.DuracionUso,
                        a.FechaRegistro
                    })
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

        //-----------------------------------------------------------------------------------------------------------------------------
        // GET: api/AsistenciasDocente/busqueda/horario/{diaSemana}/{horaentrada}/{horasalida}
        // Obtener asistencias de estudiantes por horario
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpGet("busqueda/horario/{diaSemana}/{horaentrada}/{horasalida}")]
        public async Task<IActionResult> ObtenerAsistenciasporHorario(string diaSemana, TimeSpan horaentrada, TimeSpan horasalida)
        {
            try
            {
                var asistencias = await _context.Asistencias
                    .Include(a => a.Usuario)
                    .Include(a => a.Maquina)
                    .Include(a => a.Laboratorio)
                    .Include(a => a.Cronograma)
                    .Where(a => a.RolRegistro.ToLower() == "estudiante"
                        && a.Cronograma != null
                        && a.Cronograma.DiaSemana.ToLower() == diaSemana.ToLower()
                        && (
                            // El cronograma se solapa con el rango horario buscado
                            (a.Cronograma.HoraInicio <= horasalida && a.Cronograma.HoraFin >= horaentrada)
                        ))
                    .OrderByDescending(a => a.FechaRegistro)
                    .Select(a => new
                    {
                        a.AsistenciaId,
                        EstudianteNombre = $"{a.Usuario.Nombre} {a.Usuario.ApellidoPaterno}",
                        a.Usuario.CorreoInstitucional,
                        LaboratorioCodigo = a.Laboratorio.CodigoLaboratorio,
                        Materia = a.Cronograma.Materia,
                        CronogramaHoraInicio = a.Cronograma.HoraInicio.ToString(@"hh\:mm"),
                        CronogramaHoraFin = a.Cronograma.HoraFin.ToString(@"hh\:mm"),
                        a.HoraIngreso,
                        a.HoraSalida,
                        a.DuracionUso,
                        a.FechaRegistro
                    })
                    .ToListAsync();

                return Ok(new
                {
                    diaBuscado = diaSemana,
                    horaInicioBuscada = horaentrada.ToString(@"hh\:mm"),
                    horaFinBuscada = horasalida.ToString(@"hh\:mm"),
                    totalAsistencias = asistencias.Count,
                    asistencias
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        // GET: api/Asistencias/busqueda/general
        // Obtener asistencias generales
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpGet("busqueda/general")]
        public async Task<IActionResult> ObtenerAsistenciasGeneralDocente()
        {
            try
            {
                var asistencias = await _context.Asistencias
                    .Include(a => a.Usuario)
                    .Include(a => a.Maquina)
                    .Include(a => a.Laboratorio)
                    .Include(a => a.Cronograma)
                    .OrderByDescending(a => a.FechaRegistro)
                    
                    .Select(a => new
                    {
                        a.AsistenciaId,
                        UsuarioNombre = $"{a.Usuario.Nombre} {a.Usuario.ApellidoPaterno}",
                        a.Usuario.CorreoInstitucional,
                        LaboratorioCodigo = a.Laboratorio.CodigoLaboratorio,
                        Materia = a.Cronograma.Materia,
                        a.HoraIngreso,
                        a.HoraSalida,
                        a.DuracionUso,
                        a.FechaRegistro
                    })
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