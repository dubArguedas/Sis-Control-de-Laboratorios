using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCLAB_API.Data;
using SCLAB_API.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

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

        // LOGIN
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Datos inválidos" });
                }

                var email = dto.CorreoInstitucional.Trim().ToLowerInvariant();

                // Seguimiento habilitado (sin AsNoTracking) para poder actualizar hash si hace falta
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.CorreoInstitucional == email);

                if (usuario == null)
                {
                    return Unauthorized(new { message = "Credenciales incorrectas" });
                }

                var isValid = VerifyPassword(dto.Password, usuario.PasswordHash, out var needsRehash, out var upgradedHash);
                if (!isValid)
                {
                    return Unauthorized(new { message = "Credenciales incorrectas" });
                }

                // Upgrade del hash si es legado o tiene menos iteraciones
                if (needsRehash && !string.IsNullOrEmpty(upgradedHash))
                {
                    usuario.PasswordHash = upgradedHash;
                    await _context.SaveChangesAsync();
                }

                var token = _jwtService.GenerateToken(
                    usuario.UsuarioId,
                    usuario.CorreoInstitucional,
                    usuario.Rol
                );

                // Cookie segura (SameSite=None requiere Secure=true)
                Response.Cookies.Append("authToken", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddHours(1),
                    Path = "/"
                });

                // También devolver el token en el body si lo consumes en Blazor WASM
                return Ok(new
                {
                    token,
                    message = "Inicio de sesión exitoso"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        // GET: api/Usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios()
        {
            try
            {
                var usuarios = await _context.Usuarios
                    .AsNoTracking()
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
                    .ToListAsync();

                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        // GET: api/Usuarios/5
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<Usuario>> GetUsuario(int id)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .AsNoTracking()
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        // POST: api/Usuarios
        //[Authorize]
        [HttpPost]
        public async Task<ActionResult<Usuario>> PostUsuario(Usuario usuario)
        {
            try
            {
                // Normalizar correo
                usuario.CorreoInstitucional = usuario.CorreoInstitucional.Trim().ToLowerInvariant();

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

                // Hash del password (PBKDF2)
                usuario.PasswordHash = HashPassword(usuario.PasswordHash);
                usuario.FechaRegistro = DateTime.UtcNow;

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
                    usuario.FechaRegistro,
                    usuario.PasswordHash
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        // PUT: api/Usuarios/5
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsuario(int id, Usuario usuario)
        {
            try
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

                var nuevoCorreo = usuario.CorreoInstitucional.Trim().ToLowerInvariant();
                if (nuevoCorreo != usuarioExistente.CorreoInstitucional)
                {
                    if (await _context.Usuarios.AnyAsync(u => u.CorreoInstitucional == nuevoCorreo))
                    {
                        return BadRequest(new { message = "El correo institucional ya existe" });
                    }
                    usuarioExistente.CorreoInstitucional = nuevoCorreo;
                }

                usuarioExistente.Nombre = usuario.Nombre;
                usuarioExistente.ApellidoPaterno = usuario.ApellidoPaterno;
                usuarioExistente.ApellidoMaterno = usuario.ApellidoMaterno;
                usuarioExistente.Rol = usuario.Rol;
                usuarioExistente.Estado = usuario.Estado;

                if (!string.IsNullOrWhiteSpace(usuario.PasswordHash))
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        // DELETE: api/Usuarios/5
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.UsuarioId == id);
        }

        // Configuración PBKDF2
        private const int Pbkdf2Iteraciones = 210_000;
        private const int SaltSize = 16;
        private const int KeySize = 32;

        // Hash seguro (PBKDF2$<iteraciones>$<salt>$<key>)
        private string HashPassword(string passwordPlain)
        {
            if (string.IsNullOrWhiteSpace(passwordPlain))
                throw new ArgumentException("La contraseña no puede estar vacía.", nameof(passwordPlain));

            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(passwordPlain, salt, Pbkdf2Iteraciones, HashAlgorithmName.SHA256);
            var key = pbkdf2.GetBytes(KeySize);

            return $"PBKDF2${Pbkdf2Iteraciones}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
        }

        // Verifica y sugiere rehash si procede (legado o iteraciones antiguas)
        private bool VerifyPassword(string passwordPlain, string stored, out bool needsRehash, out string? upgradedHash)
        {
            needsRehash = false;
            upgradedHash = null;

            if (string.IsNullOrEmpty(stored) || string.IsNullOrEmpty(passwordPlain))
                return false;

            // PBKDF2
            if (stored.StartsWith("PBKDF2$", StringComparison.Ordinal))
            {
                var parts = stored.Split('$', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 4) return false;

                var iter = int.Parse(parts[1]);
                var salt = Convert.FromBase64String(parts[2]);
                var keyStored = Convert.FromBase64String(parts[3]);

                using var pbkdf2 = new Rfc2898DeriveBytes(passwordPlain, salt, iter, HashAlgorithmName.SHA256);
                var keyComputed = pbkdf2.GetBytes(keyStored.Length);

                var ok = CryptographicOperations.FixedTimeEquals(keyStored, keyComputed);

                // Si valida pero las iteraciones son menores al estándar actual, forzamos upgrade
                if (ok && iter < Pbkdf2Iteraciones)
                {
                    needsRehash = true;
                    upgradedHash = HashPassword(passwordPlain);
                }

                return ok;
            }

            // Legado: SHA-256 Base64
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(passwordPlain));
            var legacy = Convert.ToBase64String(hashedBytes);

            var okLegacy = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(legacy),
                Encoding.UTF8.GetBytes(stored)
            );

            if (okLegacy)
            {
                // Upgrade a PBKDF2
                needsRehash = true;
                upgradedHash = HashPassword(passwordPlain);
            }

            return okLegacy;
        }

        public class LoginDto
        {
            [Required, EmailAddress]
            public string CorreoInstitucional { get; set; }

            [Required]
            public string Password { get; set; }
        }
    }
}