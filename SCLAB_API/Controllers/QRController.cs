using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using SCLAB_API.Data;
using SCLAB_API.Models;

namespace SCLAB_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QrController : ControllerBase
    {
        private readonly SisComputoDbContext _context;
        private readonly IConfiguration _config;

        public QrController(SisComputoDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPut("generar/{maquinaId}")]
        public async Task<IActionResult> GenerarQr(int maquinaId)
        {
            try
            {
                var maquina = await _context.Maquinas
                    .Include(m => m.Laboratorio) 
                    .FirstOrDefaultAsync(m => m.MaquinaId == maquinaId);
                if (maquina == null)
                    return NotFound(new { message = $"Máquina con ID {maquinaId} no encontrada." });
                var codigoLaboratorio = maquina.Laboratorio?.CodigoLaboratorio ?? "N/A";


                var baseUrl = _config["FrontendBaseUrl"] ?? "https://localhost:7219";

                var url = $"{baseUrl}/maquina-formulario?" +
                          $"codigoMaquina={Uri.EscapeDataString(maquina.CodigoMaquina)}&" +
                          $"codigoLaboratorio={Uri.EscapeDataString(codigoLaboratorio)}";

                using var generator = new QRCodeGenerator();
                using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                using var png = new PngByteQRCode(data);
                var qrBytes = png.GetGraphic(20);

                maquina.Qr = qrBytes;
                await _context.SaveChangesAsync();

                return File(qrBytes, "image/png", fileDownloadName: $"{maquina.CodigoMaquina}_QR.png");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al generar QR", detail = ex.Message });
            }
        }
    }
}
