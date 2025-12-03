using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCLAB_API.Data;
using SCLAB_API.Models;

namespace SCLAB_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class CronogramaController : ControllerBase
    {
        private readonly SisComputoDbContext _context;

        public CronogramaController(SisComputoDbContext context)
        {
            _context = context;
        }
        /*explicaion
         * Explicación breve:
        GET: retorna todo el cronograma de un laboratorio, ordenado por día y hora.
        Incluye un campo calculado Estado = "libre" si Materia es null.

        PUT: actualiza solo el campo Materia.
        Si lo dejas null, marca como “libre”; si tiene texto, como “ocupado”.

        Sin POST ni DELETE, porque el cronograma es estático.*/

        // GET: api/Cronograma/laboratorio/{laboratorioId}
        // Listar el cronograma completo de un laboratorio
        [HttpGet("laboratorio/{laboratorioId}")]
        public async Task<IActionResult> ObtenerCronogramaPorLaboratorio(int laboratorioId)
        {
            try
            {
                var laboratorio = await _context.Laboratorios
                    .Include(l => l.Cronogramas)
                    .FirstOrDefaultAsync(l => l.LaboratorioId == laboratorioId);

                if (laboratorio == null)
                    return NotFound(new { message = "Laboratorio no encontrado." });

                var cronograma = await _context.CronogramaIntervals
                    .Where(c => c.LaboratorioId == laboratorioId)
                    .OrderBy(c => c.DiaSemana)
                    .ThenBy(c => c.HoraInicio)
                    .Select(c => new
                    {
                        c.CronogramaId,
                        c.DiaSemana,
                        c.HoraInicio,
                        c.HoraFin,
                        c.Materia,
                        Estado = string.IsNullOrEmpty(c.Materia) ? "libre" : "ocupado"
                    })
                    .ToListAsync();

                return Ok(new
                {
                    laboratorio.LaboratorioId,
                    laboratorio.CodigoLaboratorio,
                    Cronograma = cronograma
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener el cronograma.", detail = ex.Message });
            }
        }

        // PUT: api/Cronograma/{id}
        // Actualizar solo la materia de un bloque
        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarMateria(int id, [FromBody] string? materia)
        {
            try
            {
                var bloque = await _context.CronogramaIntervals.FindAsync(id);
                if (bloque == null)
                    return NotFound(new { message = "Bloque de cronograma no encontrado." });

                // Si materia = null => libre
                bloque.Materia = string.IsNullOrWhiteSpace(materia) ? null : materia.Trim().ToUpper();
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = bloque.Materia == null
                        ? "Bloque marcado como libre."
                        : $"Bloque asignado a la materia '{bloque.Materia}'.",
                    bloque.CronogramaId,
                    bloque.DiaSemana,
                    bloque.HoraInicio,
                    bloque.HoraFin,
                    bloque.Materia
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar el bloque del cronograma.", detail = ex.Message });
            }
        }


        // GET: api/Cronograma/materias
        [HttpGet("materias")]
        public async Task<IActionResult> ObtenerMateriasUnicas()
        {
            try
            {
                // Obtiene todas las materias distintas que no sean nulas ni vacías
                var materias = await _context.CronogramaIntervals
                    .Where(c => !string.IsNullOrEmpty(c.Materia))
                    .Select(c => c.Materia)
                    .Distinct()
                    .OrderBy(m => m)
                    .ToListAsync();

                return Ok(materias);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener materias", detail = ex.Message });
            }
        }
    }
}
