using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using SCLAB_API.Data;
using SCLAB_API.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
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
        // -----------------------------------------------------------------------------------------------------------------------------
        // Clase para el login
        // -----------------------------------------------------------------------------------------------------------------------------
        public class LoginDto
        {
            [Required, EmailAddress]
            public string CorreoInstitucional { get; set; } = string.Empty!;

            [Required]
            public string Password { get; set; } = string.Empty!;


        }

        // LOGIN como tal solo se queriere que se mande un objeto que tengan los campos de correo y password, se valida y se devuelve un token JWT, el token es entregado en dos maneras
        // en cockie (por revisar) y por la respuesta en el body
        //-----------------------------------------------------------------------------------------------------------------------------
        // LOGIN
        //-----------------------------------------------------------------------------------------------------------------------------
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

                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.CorreoInstitucional == email);

                

                if (usuario == null)
                {
                    return Unauthorized(new { message = "Credenciales incorrectas" });
                }

                if (usuario.Estado == "inactivo")
                {
                    return Unauthorized(new { message = "El usuario está inactivo" });
                }

                var isValid = VerifyPassword(dto.Password, usuario.PasswordHash, out var needsRehash, out var upgradedHash);
                if (!isValid)
                {
                    return Unauthorized(new { message = "Credenciales incorrectas" });
                }

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

                // Solo devolver el token en el body
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



        // GET : listas filtradas por rol, como tal el objetivo es que los 4 endpoinds sean consumidos para obtener la lista completa, cada terminacion de endpoind indica la el rol
        // de la lista que devuelve, lo ideal es que se invoquen los 4 pero en casos como el panel de docentes que solo podran ver una lista solo se invoca una, en vez de llamar a una
        // sola lista esto las filtra de manera directa y las devuelve en 4 diferentes listas ya filtradas por rol

        //utilizar:

        // GET: api/Usuarios/estudiante
        // GET: api/Usuarios/docente
        // GET: api/Usuarios/encargado
        // GET: api/Usuarios/admin

        //para obtener la lista completa de los usuarios en la bd

        //-----------------------------------------------------------------------------------------------------------------------------
        // GET: api/Usuarios/estudiante
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpGet("estudiante")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuariosEstudiantes()
        {
            try
            {
                var usuariosfiltrados = await _context.Usuarios
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
                    }).Where(p => p.Rol == "estudiante")
                    .ToListAsync();

                return Ok(usuariosfiltrados);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }
        //-----------------------------------------------------------------------------------------------------------------------------
        // GET: api/Usuarios/docente
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpGet("docente")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuariosDocente()
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
                    }).Where(p => p.Rol == "docente")
                    .ToListAsync();

                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }
        //-----------------------------------------------------------------------------------------------------------------------------
        // GET: api/Usuarios/encargado
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpGet("encargado")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuariosEncargado()
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
                    }).Where(p => p.Rol == "encargado")
                    .ToListAsync();

                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }
        //-----------------------------------------------------------------------------------------------------------------------------
        // GET: api/Usuarios/encargado
        //-----------------------------------------------------------------------------------------------------------------------------
        [HttpGet("admin")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuariosAdmin()
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
                    }).Where(p => p.Rol == "admin")
                    .ToListAsync();

                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }
        //-----------------------------------------------------------------------------------------------------------------------------

        //Yo John, estoy creando este endpoint para poder hacer la lista sin token----------
        //-----------------------------------------------------------------------------------------------------------------------------
        // GET: api/Usuarios (PÚBLICO - sin autorización)
        //-----------------------------------------------------------------------------------------------------------------------------
        //[AllowAnonymous]
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuariosPublico()
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
        // GET filtrado por id solo devuelve un usuario especifico
        //-----------------------------------------------------------------------------------------------------------------------------
        // GET: api/Usuarios/5
        //-----------------------------------------------------------------------------------------------------------------------------
        
        [HttpGet("{id}")]
        [Authorize]
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
        // POST crea un nuevo usuario utilizando las validaciones de ci unico, correo unico y hash de password especificado en los reuquerimientos de usuario
        //-----------------------------------------------------------------------------------------------------------------------------
        // POST: api/Usuarios
        //-----------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Usuario>> PostUsuario(Usuario usuario)
        {
            try
            {
                // Normalizar correo
                //usuario.CorreoInstitucional = usuario.CorreoInstitucional.Trim().ToLowerInvariant();

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

                //Configuracion de Estado por defecto
                usuario.Estado = "activo";

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

        // PUT POR CONVENCION Y REQUERIMIENTOS DE USUARIO, SOLO EL ADMIN PUEDE MODIFICAR EL PASSWORD DE OTROS USUARIOS, ADEMAS DE QUE COMO TAL LOS DATOS DEL USUARIO NO PUEDEN
        // SER EDITADOS A EXEPCION DE NOMBRE Y APELLIDOS, POR LO TANTO ESTE ENDPOINT SOLO PERMITE ESO, EL PASSWORD SOLO SERA MODIFICADO POR EL ADMINISTRADOR, ESO QUEDARA A CONSIDERACION 
        // DE FRONT YA QUE SE ESTA ELIMINANDO EL ACCESO POR ROLES
        //-----------------------------------------------------------------------------------------------------------------------------
        // PUT: api/Usuarios/5
        //-----------------------------------------------------------------------------------------------------------------------------
        
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutUsuario(int id, Usuario usuario)
        {
            try
            {
                //var rolActual = User.FindFirstValue(ClaimTypes.Role);
                if (id != usuario.UsuarioId)
                {
                    return BadRequest(new { message = "El ID no coincide" });
                }

                var usuarioExistente = await _context.Usuarios.FindAsync(id);

                if (usuarioExistente == null)
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                //DATOS UNICOS QUE PUEDES SER EDITABLES, LOS DEMAS CON REQUERIMIENTOS NO SE PUEDEN MODIFICAR
                usuarioExistente.Nombre = usuario.Nombre;
                usuarioExistente.ApellidoPaterno = usuario.ApellidoPaterno;
                usuarioExistente.ApellidoMaterno = usuario.ApellidoMaterno;


                //ACTUALIZACION DIRECTA DEL PASSWORD, SOLO EL ADMIN PUEDE HACER ESTO PERO AUN NO SE ENCUENTRA HABILITADO O EN TODO CASO SOLO PODRA SER ACCESIBLE DEPENDIENDO DEL
                // PANEL EN EL QUE SE ENCUENTRE, EN EL PANEL DE ADMINISTRADOR PODRA HACER ESTO, EN LOS DEMAS NO
                usuarioExistente.PasswordHash = HashPassword(usuario.PasswordHash);
                

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

        // DELETE LOGICO, CAMBIA EL ESTADO A INACTIVO, NO ELIMINA EL REGISTRO DE LA BASE DE DATOS
        //-----------------------------------------------------------------------------------------------------------------------------
        // DELETE: api/Usuarios/5
        //-----------------------------------------------------------------------------------------------------------------------------
        
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario == null)
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                usuario.Estado = "inactivo";

                await _context.SaveChangesAsync();

                return Ok(new { message = "Usuario eliminado de manera logica correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        // ACTIVACION LOGICO, CAMBIA EL ESTADO A INACTIVO, NO ELIMINA EL REGISTRO DE LA BASE DE DATOS
        //-----------------------------------------------------------------------------------------------------------------------------
        // PUT: api/Usuarios/activo/5
        //-----------------------------------------------------------------------------------------------------------------------------

        [HttpPut ("activo/{id}")]
        [Authorize]
        public async Task<IActionResult> ActiveUsuario(int id)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario == null)
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                usuario.Estado = "activo";

                await _context.SaveChangesAsync();

                return Ok(new { message = "Usuario activado de manera logica correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }



        //-----------------------------------------------------------------------------------------------------------------------------
        //FUNCIONES DE VERIFICACION Y HASH NO ES NECESARIO MODIFICARLAS
        //-----------------------------------------------------------------------------------------------------------------------------

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

        



    }
}