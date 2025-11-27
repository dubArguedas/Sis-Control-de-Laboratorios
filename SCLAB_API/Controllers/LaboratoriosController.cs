using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCLAB_API.Data;
using SCLAB_API.Models;
using SCLAB_Entities;

namespace SCLAB_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LaboratoriosController : ControllerBase
    {
        private readonly SisComputoDbContext _context;

        public LaboratoriosController(SisComputoDbContext context)
        {
            _context = context;
        }

        
        // POST: api/Laboratorios
        // la creacion del cronograma base es automatica al crear el laboratorio, siendo que las materias estan en null
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> CrearLaboratorio([FromBody] Laboratorio laboratorio)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (await _context.Laboratorios.AnyAsync(l => l.CodigoLaboratorio == laboratorio.CodigoLaboratorio))
                    return BadRequest(new { message = "El código de laboratorio ya existe." });

                var ubicacionValida = new[] { "torre_maestra", "torre_innovacion" };
                if (!ubicacionValida.Contains(laboratorio.Ubicacion.ToLower()))
                    return BadRequest(new { message = "Ubicación inválida. Solo torre_maestra o torre_innovacion." });

                laboratorio.Estado = "libre";
                laboratorio.FechaRegistro = DateTime.Now;

                _context.Laboratorios.Add(laboratorio);
                await _context.SaveChangesAsync(); 

                var dias = new[] { "lunes", "martes", "miercoles", "jueves", "viernes", "sabado" };
                var intervalos = new (TimeSpan inicio, TimeSpan fin)[]
                {
                (new TimeSpan(7,30,0), new TimeSpan(9,10,0)),
                (new TimeSpan(9,20,0),  new TimeSpan(11,0,0)),
                (new TimeSpan(11,10,0),  new TimeSpan(12,50,0)),
                (new TimeSpan(13,0,0),  new TimeSpan(14,40,0)),
                (new TimeSpan(14,50,0),  new TimeSpan(16,30,0)),
                (new TimeSpan(16,40,0), new TimeSpan(18,20,0)),
                (new TimeSpan(18,30,0),  new TimeSpan(20,10,0)),
                (new TimeSpan(20,20,0),  new TimeSpan(22,0,0))
                };

                var cronogramas = new List<CronogramaInterval>();

                foreach (var dia in dias)
                {
                    foreach (var intervalo in intervalos)
                    {
                        cronogramas.Add(new CronogramaInterval
                        {
                            LaboratorioId = laboratorio.LaboratorioId,
                            DiaSemana = dia,
                            HoraInicio = intervalo.inicio,
                            HoraFin = intervalo.fin,
                            Materia = null,
                        });
                    }
                }

                await _context.CronogramaIntervals.AddRangeAsync(cronogramas);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return CreatedAtAction(nameof(ObtenerLaboratorio), new { id = laboratorio.LaboratorioId }, new
                {
                    message = "Laboratorio creado exitosamente con cronograma base generado.",
                    laboratorio.LaboratorioId,
                    laboratorio.CodigoLaboratorio,
                    laboratorio.Ubicacion,
                    laboratorio.Estado
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Error al crear el laboratorio y su cronograma.", detail = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LaboratorioListCLS>>> ListarLaboratorios()
        {
            try
            {
                var laboratorios = await _context.Laboratorios
                    .Select(l => new LaboratorioListCLS
                    {
                        LaboratorioId = l.LaboratorioId,
                        CodigoLaboratorio = l.CodigoLaboratorio,
                        Ubicacion = l.Ubicacion,
                        Capacidad = l.Capacidad,
                        Estado = l.Estado, 
                        FechaRegistro = l.FechaRegistro
                    })
                    .ToListAsync();

                return Ok(laboratorios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener los laboratorios.", detail = ex.Message });
            }
        }

        // GET: api/Laboratorios/{id}
        // Obtener un laboratorio con sus máquinas
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerLaboratorio(int id)
        {
            try
            {
                var laboratorio = await _context.Laboratorios
                    .Include(l => l.Maquinas)
                    .Where(l => l.LaboratorioId == id)
                    .Select(l => new
                    {
                        l.LaboratorioId,
                        l.CodigoLaboratorio,
                        l.Ubicacion,
                        l.Capacidad,
                        l.Estado,
                        l.FechaRegistro,
                        Maquinas = l.Maquinas.Select(m => new
                        {
                            m.MaquinaId,
                            m.CodigoMaquina,
                            m.Estado,
                            m.DescripcionHardware
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (laboratorio == null)
                    return NotFound(new { message = "Laboratorio no encontrado." });

                return Ok(laboratorio);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener el laboratorio.", detail = ex.Message });
            }
        }

        // PUT: api/Laboratorios/{id}/estado
        // Actualizar solo el estado del laboratorio
        [Authorize(Roles = "admin")]
        [HttpPut("{id}/estado")]
        public async Task<IActionResult> ActualizarEstado(int id, [FromBody] string nuevoEstado)
        {
            try
            {
                var laboratorio = await _context.Laboratorios.FindAsync(id);
                if (laboratorio == null)
                    return NotFound(new { message = "Laboratorio no encontrado." });

                var estadosValidos = new[] { "libre", "ocupado", "cerrado" };
                if (!estadosValidos.Contains(nuevoEstado.ToLower()))
                    return BadRequest(new { message = "Estado inválido. Valores permitidos: libre, ocupado, cerrado." });

                laboratorio.Estado = nuevoEstado.ToLower();
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Estado del laboratorio actualizado a '{nuevoEstado}'." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar el estado del laboratorio.", detail = ex.Message });
            }
        }

        // DELETE: api/Laboratorios/{id}
        // No elimina físicamente, marca como cerrado
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> CerrarLaboratorio(int id)
        {
            try
            {
                var laboratorio = await _context.Laboratorios.FindAsync(id);
                if (laboratorio == null)
                    return NotFound(new { message = "Laboratorio no encontrado." });

                laboratorio.Estado = "cerrado";
                await _context.SaveChangesAsync();

                return Ok(new { message = "El laboratorio ha sido marcado como cerrado (no eliminado físicamente)." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al cerrar el laboratorio.", detail = ex.Message });
            }
        }

        // PATCH: api/Laboratorios/{id}/recalcular-capacidad
        // Recalcula capacidad (según cantidad de máquinas registradas)
        [HttpPatch("{id}/recalcular-capacidad")]
        public async Task<IActionResult> RecalcularCapacidad(int id)
        {
            try
            {
                var laboratorio = await _context.Laboratorios.FindAsync(id);
                if (laboratorio == null)
                    return NotFound(new { message = "Laboratorio no encontrado." });

                laboratorio.Capacidad = await _context.Maquinas.CountAsync(m => m.LaboratorioId == id);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Capacidad actualizada correctamente. Total de máquinas: {laboratorio.Capacidad}",
                    laboratorio.LaboratorioId,
                    laboratorio.Capacidad
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al recalcular la capacidad.", detail = ex.Message });
            }
        }
        //metodo de creacion automatica de cronograma por defecto con todas las materias en libre


    }
}
