using Microsoft.JSInterop;
using SCLAB_Client.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace SCLAB_Client.Services
{
    public class UsuarioService
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime _jsRuntime;

        public UsuarioService(HttpClient http, IJSRuntime jsRuntime)
        {
            _http = http;
            _jsRuntime = jsRuntime;
        }

        #region GET - Listar Usuarios

        /// <summary>
        /// GET /api/Usuarios
        /// Obtiene todos los usuarios según el rol del usuario autenticado
        /// IMPORTANTE: La respuesta varía según el rol
        /// </summary>
        public async Task<List<UsuarioDto>>GetUsuariosAsync()
        {
            try
            {
                var response = await _http.GetAsync("api/Usuarios");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    return new List<UsuarioDto>();
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Respuesta API: {jsonString}"); // Para debug

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Intentar deserializar directamente primero
                try
                {
                    var usuarios = JsonSerializer.Deserialize<List<UsuarioDto>>(jsonString, options);
                    return usuarios ?? new List<UsuarioDto>();
                }
                catch (JsonException)
                {
                    // Si falla, intentar como objeto con propiedades separadas (para encargado)
                    try
                    {
                        var responseObj = JsonSerializer.Deserialize<UsuarioResponse>(jsonString, options);
                        var todosUsuarios = new List<UsuarioDto>();

                        if (responseObj?.UsuariosEstudiantes != null)
                            todosUsuarios.AddRange(responseObj.UsuariosEstudiantes);
                        if (responseObj?.UsuariosDocentes != null)
                            todosUsuarios.AddRange(responseObj.UsuariosDocentes);
                        if (responseObj?.UsuariosEncargado != null)
                            todosUsuarios.AddRange(responseObj.UsuariosEncargado);

                        return todosUsuarios;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al parsear respuesta: {ex.Message}");
                        return new List<UsuarioDto>();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetUsuariosAsync: {ex.Message}");
                return new List<UsuarioDto>();
            }
        }

        // Clase auxiliar para parsear la respuesta del encargado
        public class UsuarioResponse
        {
            public List<UsuarioDto> UsuariosEstudiantes { get; set; } = new();
            public List<UsuarioDto> UsuariosDocentes { get; set; } = new();
            public List<UsuarioDto> UsuariosEncargado { get; set; } = new();
        }
        #endregion

        #region GET - Obtener Usuario por ID

        /// <summary>
        /// GET /api/Usuarios/{id}
        /// Obtiene un usuario específico por su ID
        /// </summary>
        public async Task<UsuarioDto?> GetUsuarioAsync(int id)
        {
            try
            {
                var response = await _http.GetAsync($"api/Usuarios/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<UsuarioDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetUsuarioAsync: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region POST - Crear Usuario

        /// <summary>
        /// POST /api/Usuarios
        /// Crea un nuevo usuario
        /// IMPORTANTE: El backend asigna automáticamente:
        /// - Estado = "activo"
        /// - FechaRegistro = DateTime.UtcNow
        /// - PasswordHash = Se hashea con PBKDF2
        /// </summary>
        public async Task<(bool Success, string Message)> CreateUsuarioAsync(UsuarioCreateDto usuarioCreate)
        {
            try
            {
                // Normalizar correo
                usuarioCreate.CorreoInstitucional = usuarioCreate.CorreoInstitucional.Trim().ToLowerInvariant();

                // Crear objeto que coincida con el modelo de la API
                var usuarioParaAPI = new
                {
                    Nombre = usuarioCreate.Nombre,
                    ApellidoPaterno = usuarioCreate.ApellidoPaterno,
                    ApellidoMaterno = usuarioCreate.ApellidoMaterno,
                    CorreoInstitucional = usuarioCreate.CorreoInstitucional,
                    CI = usuarioCreate.CI,
                    Rol = usuarioCreate.Rol,
                    PasswordHash = usuarioCreate.PasswordHash // La API lo hasheará automáticamente
                                                              // Estado y FechaRegistro se asignan en el backend
                };

                var response = await _http.PostAsJsonAsync("api/Usuarios", usuarioParaAPI);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Usuario creado exitosamente");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error del API: {errorContent}"); // Para debug

                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(
                            errorContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        return (false, errorResponse?.message ?? "Error al crear usuario");
                    }
                    catch
                    {
                        return (false, errorContent);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Excepción: {ex.Message}");
                return (false, $"Error de conexión: {ex.Message}");
            }
        }
        #endregion

        #region PUT - Actualizar Usuario

        /// <summary>
        /// PUT /api/Usuarios/{id}
        /// Actualiza un usuario existente
        /// IMPORTANTE: El backend solo actualiza:
        /// - Nombre
        /// - ApellidoPaterno
        /// - ApellidoMaterno
        /// NO se puede cambiar: CI, Correo, Rol, FechaRegistro
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateUsuarioAsync(int id, UsuarioDto usuario)
        {
            try
            {
                // Crear el objeto que espera el backend (Usuario completo)
                var usuarioUpdate = new
                {
                    UsuarioId = usuario.UsuarioId,
                    Nombre = usuario.Nombre,
                    ApellidoPaterno = usuario.ApellidoPaterno,
                    ApellidoMaterno = usuario.ApellidoMaterno,
                    CorreoInstitucional = usuario.CorreoInstitucional,
                    CI = usuario.CI,
                    Rol = usuario.Rol,
                    Estado = usuario.Estado,
                    FechaRegistro = usuario.FechaRegistro,
                    PasswordHash = "" // Vacío para no actualizar contraseña
                };

                var response = await _http.PutAsJsonAsync($"api/Usuarios/{id}", usuarioUpdate);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Usuario actualizado correctamente");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(
                            errorContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        return (false, errorResponse?.message ?? "Error al actualizar usuario");
                    }
                    catch
                    {
                        return (false, errorContent);
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error de conexión: {ex.Message}");
            }
        }

        #endregion

        #region DELETE - Cambiar Estado (Eliminación Lógica)

        /// <summary>
        /// DELETE /api/Usuarios/{id}
        /// Cambia el estado del usuario de "activo" a "inactivo" (eliminación lógica)
        /// </summary>
        public async Task<(bool Success, string Message)> ToggleUsuarioEstadoAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"api/Usuarios/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Estado del usuario actualizado correctamente");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(
                            errorContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        return (false, errorResponse?.message ?? "Error al cambiar estado");
                    }
                    catch
                    {
                        return (false, errorContent);
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error de conexión: {ex.Message}");
            }
        }

        //#endregion

        //#region LOGIN

        ///// <summary>
        ///// POST /api/Usuarios/login
        ///// Autentica un usuario y devuelve un token JWT
        ///// </summary>
        //public async Task<(bool Success, string Token, string Message)> LoginAsync(string correo, string password)
        //{
        //    try
        //    {
        //        var loginData = new
        //        {
        //            CorreoInstitucional = correo.Trim().ToLowerInvariant(),
        //            Password = password
        //        };

        //        var response = await _http.PostAsJsonAsync("api/Usuarios/login", loginData);

        //        if (response.IsSuccessStatusCode)
        //        {
        //            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        //            return (true, loginResponse?.token ?? "", loginResponse?.message ?? "Login exitoso");
        //        }
        //        else
        //        {
        //            var errorContent = await response.Content.ReadAsStringAsync();

        //            try
        //            {
        //                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(
        //                    errorContent,
        //                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        //                return (false, "", errorResponse?.message ?? "Credenciales incorrectas");
        //            }
        //            catch
        //            {
        //                return (false, "", "Error al iniciar sesión");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return (false, "", $"Error de conexión: {ex.Message}");
        //    }
        //}

        #endregion
    }
}