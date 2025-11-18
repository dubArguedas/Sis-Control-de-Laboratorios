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
    public class MaquinasController : ControllerBase
    {
        private readonly SisComputoDbContext _context;

        public MaquinasController(SisComputoDbContext context)
        {
            _context = context;
        }

        // POST: api/Maquinas
        // Crear nueva máquina
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> CrearMaquina([FromBody] Maquina maquina)
        {
            try
            {
                Console.WriteLine($"Recibido Maquina: LaboratorioId={maquina.LaboratorioId}, Descripcion={maquina.DescripcionHardware}, CodigoMaquina={maquina.CodigoMaquina}");

                var laboratorio = await _context.Laboratorios.FindAsync(maquina.LaboratorioId);
                if (laboratorio == null)
                    return BadRequest(new { message = "El laboratorio especificado no existe." });

                var codigoLab = laboratorio.CodigoLaboratorio.ToUpper();
                var totalEnLaboratorio = await _context.Maquinas
                    .CountAsync(m => m.LaboratorioId == laboratorio.LaboratorioId);

                maquina.CodigoMaquina = $"{codigoLab}-{totalEnLaboratorio + 1}";
                maquina.Estado = "disponible";
                maquina.FechaRegistro = DateTime.Now;

                _context.Maquinas.Add(maquina);
                await _context.SaveChangesAsync();

                laboratorio.Capacidad = await _context.Maquinas.CountAsync(m => m.LaboratorioId == laboratorio.LaboratorioId);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(ObtenerMaquina), new { id = maquina.MaquinaId }, new
                {
                    maquina.MaquinaId,
                    maquina.CodigoMaquina,
                    maquina.DescripcionHardware,
                    maquina.Estado,
                    maquina.LaboratorioId,
                    maquina.FechaRegistro
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al guardar: " + ex);
                return StatusCode(500, new { message = "Error al crear la máquina.", detail = ex.Message });
            }
        }

        // GET: api/Maquinas/{id}
        // Obtener una máquina en específico
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerMaquina(int id)
        {
            try
            {
                var maquina = await _context.Maquinas
                    .Include(m => m.Laboratorio)
                    .Where(m => m.MaquinaId == id)
                    .Select(m => new
                    {
                        m.MaquinaId,
                        m.CodigoMaquina,
                        m.DescripcionHardware,
                        m.Estado,
                        m.FechaRegistro,
                        m.Qr,
                        Laboratorio = new
                        {
                            m.Laboratorio.LaboratorioId,
                            m.Laboratorio.CodigoLaboratorio,
                            m.Laboratorio.Ubicacion,
                            m.Laboratorio.Estado
                        }
                    })
                    .FirstOrDefaultAsync();

                if (maquina == null)
                    return NotFound(new { message = "Máquina no encontrada." });

                return Ok(maquina);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener la máquina.", detail = ex.Message });
            }
        }

        // PUT: api/Maquinas/{id}
        // Actualizar descripción o estado
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarMaquina(int id, [FromBody] Maquina maquina)
        {
            try
            {
                var existente = await _context.Maquinas.FindAsync(id);
                if (existente == null)
                    return NotFound(new { message = "Máquina no encontrada." });

                // Actualizar solo descripción y estado
                if (!string.IsNullOrWhiteSpace(maquina.DescripcionHardware))
                    existente.DescripcionHardware = maquina.DescripcionHardware;

                if (!string.IsNullOrWhiteSpace(maquina.Estado))
                {
                    var estadosValidos = new[] { "libre", "ocupado", "mantenimiento", "descontinuado" };
                    if (!estadosValidos.Contains(maquina.Estado.ToLower()))
                        return BadRequest(new { message = "Estado inválido. Solo: libre, ocupado, mantenimiento, descontinuado" });

                    existente.Estado = maquina.Estado.ToLower();
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Máquina actualizada correctamente.",
                    existente.MaquinaId,
                    existente.CodigoMaquina,
                    existente.DescripcionHardware,
                    existente.Estado
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar la máquina.", detail = ex.Message });
            }
        }

        // DELETE: api/Maquinas/{id}
        // Eliminar máquina físicamente //REVISAR Y CONSULTAR
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarMaquina(int id)
        {
            try
            {
                var maquina = await _context.Maquinas.FindAsync(id);
                if (maquina == null)
                    return NotFound(new { message = "Máquina no encontrada." });

                var laboratorioId = maquina.LaboratorioId;

                if (maquina.Estado == "descontinuado")
                {
                    return BadRequest(new { message = "La máquina ya se encuentra descontinuada." });
                }

                maquina.Estado = "descontinuado";

                _context.Maquinas.Update(maquina);
                await _context.SaveChangesAsync();

                var laboratorio = await _context.Laboratorios.FindAsync(laboratorioId);
                if (laboratorio != null)
                {
                    laboratorio.Capacidad = await _context.Maquinas
                        .CountAsync(m => m.LaboratorioId == laboratorioId && m.Estado != "descontinuado");
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = "Máquina descontinuada (borrado lógico) y capacidad del laboratorio actualizada.",
                    maquina.MaquinaId,
                    maquina.CodigoMaquina,
                    NuevoEstado = maquina.Estado
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al descontinuar la máquina.", detail = ex.Message });
            }
        }


        // GET: api/Maquinas
        // Listar todas las máquinas (con info del laboratorio)
        [HttpGet]
        public async Task<IActionResult> ListarMaquinas()
        {
            try
            {
                var lista = await _context.Maquinas
                    .Include(m => m.Laboratorio)
                    .Select(m => new
                    {
                        m.MaquinaId,
                        m.CodigoMaquina,
                        m.LaboratorioId,
                        m.DescripcionHardware,
                        m.Estado,
                        m.FechaRegistro,
                        LaboratorioCodigo = m.Laboratorio.CodigoLaboratorio
                    })
                    .ToListAsync();

                return Ok(lista);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al listar máquinas.", detail = ex.Message });
            }
        }

    }

}
