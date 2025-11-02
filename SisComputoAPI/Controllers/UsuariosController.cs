using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCLAB_API.Data;
using SCLAB_API.Models;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SCLAB_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly IJwtService _jwtService;
        private readonly SisComputoDbContext _context;

        public UsuariosController(SisComputoDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var validation = await _context.Usuarios.Where(u => u.CorreoInstitucional == dto.CorreoInstitucional 
            && u.PasswordHash == dto.PasswordHash).FirstOrDefaultAsync();

            // Validación
            if (validation == null)
            {
                return Unauthorized("Credenciales incorrectas");
            }
            else
            {
                var token = _jwtService.GenerateToken(
                    validation.UsuarioId,
                    validation.CorreoInstitucional,
                    validation.Rol
                );

                return Ok(new
                {
                    message = "Inicio de sesión exitoso",
                    token = token
                });
            }


        }

        // GET: api/Usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios()
        {
            var usuarios = await _context.Usuarios
                .Select(u => new
                {
                    u.UsuarioId,
                    u.Nombre,
                    u.ApellidoPaterno,
                    u.ApellidoMaterno,
                    u.CorreoInstitucional,
                    u.CI,
                    u.PasswordHash,
                    u.Rol,
                    u.Estado,
                    u.FechaRegistro
                })
                .ToListAsync();

            return Ok(usuarios);
        }

        // GET: api/Usuarios/5
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<Usuario>> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios
                .Where(u => u.UsuarioId == id)
                .Select(u => new
                {
                    u.UsuarioId,
                    u.Nombre,
                    u.ApellidoPaterno,
                    u.ApellidoMaterno,
                    u.CorreoInstitucional,
                    u.CI,
                    u.Rol,
                    u.Estado,
                    u.PasswordHash,
                    u.FechaRegistro
                })
                .FirstOrDefaultAsync();

            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            return Ok(usuario);
        }





        // POST: api/Usuarios
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Usuario>> PostUsuario(Usuario usuario)
        {
            // Validar correo único
            if (await _context.Usuarios.AnyAsync(u => u.CorreoInstitucional == usuario.CorreoInstitucional))
            {
                return BadRequest(new { message = "El correo institucional ya existe" });
            }

            // Validar CI único
            if (await _context.Usuarios.AnyAsync(u => u.CI == usuario.CI))
            {
                return BadRequest(new { message = "El CI ya existe" });
            }

            // Hash del password
            usuario.PasswordHash = HashPassword(usuario.PasswordHash);
            usuario.FechaRegistro = DateTime.Now;

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUsuario), new { id = usuario.UsuarioId }, new
            {
                usuario.UsuarioId,
                usuario.Nombre,
                usuario.ApellidoPaterno,
                usuario.ApellidoMaterno,
                usuario.CorreoInstitucional,
                usuario.CI,
                usuario.Rol,
                usuario.Estado,
                usuario.PasswordHash,
                usuario.FechaRegistro
            });
        }

        // PUT: api/Usuarios/5
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsuario(int id, Usuario usuario)
        {
            if (id != usuario.UsuarioId)
            {
                return BadRequest(new { message = "El ID no coincide" });
            }

            var usuarioExistente = await _context.Usuarios.FindAsync(id);
            if (usuarioExistente == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            // Validar correo único (si cambió)
            if (usuario.CorreoInstitucional != usuarioExistente.CorreoInstitucional)
            {
                if (await _context.Usuarios.AnyAsync(u => u.CorreoInstitucional == usuario.CorreoInstitucional))
                {
                    return BadRequest(new { message = "El correo institucional ya existe" });
                }
            }

            // Actualizar campos
            usuarioExistente.Nombre = usuario.Nombre;
            usuarioExistente.ApellidoPaterno = usuario.ApellidoPaterno;
            usuarioExistente.ApellidoMaterno = usuario.ApellidoMaterno;
            usuarioExistente.CorreoInstitucional = usuario.CorreoInstitucional;
            usuarioExistente.Rol = usuario.Rol;
            usuarioExistente.Estado = usuario.Estado;
            // Mantener el mismo password 
            usuarioExistente.PasswordHash = usuario.PasswordHash;

            // Si se envía un nuevo password, actualizarlo
            if (!string.IsNullOrEmpty(usuario.PasswordHash) && usuario.PasswordHash != usuarioExistente.PasswordHash)
            {
                usuarioExistente.PasswordHash = HashPassword(usuario.PasswordHash);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsuarioExists(id))
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }
                throw;
            }

            return Ok(new { message = "Usuario actualizado correctamente" });
        }

        // DELETE: api/Usuarios/5
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Usuario eliminado correctamente" });
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.UsuarioId == id);
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }






        public class LoginDto
        {
            public string CorreoInstitucional { get; set; }
            public string PasswordHash { get; set; }
        }
    }
}