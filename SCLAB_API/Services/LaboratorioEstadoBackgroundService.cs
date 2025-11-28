using Microsoft.EntityFrameworkCore;
using SCLAB_API.Data;
using SCLAB_API.Models;

namespace SCLAB_API.Services
{
    public class LaboratorioEstadoBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LaboratorioEstadoBackgroundService> _logger;
        private readonly TimeSpan _intervalo = TimeSpan.FromSeconds(30); // Revisar cada 30 segundos para mayor precisión

        // ✅ HORARIOS EXACTOS DE CAMBIO DE BLOQUE
        private readonly TimeSpan[] _horariosFinBloque = new[]
        {
            new TimeSpan(9, 10, 0),   // Fin del bloque 07:30-09:10
            new TimeSpan(11, 0, 0),   // Fin del bloque 09:20-11:00
            new TimeSpan(12, 50, 0),  // Fin del bloque 11:10-12:50
            new TimeSpan(14, 40, 0),  // Fin del bloque 13:00-14:40
            new TimeSpan(16, 30, 0),  // Fin del bloque 14:50-16:30
            new TimeSpan(18, 20, 0),  // Fin del bloque 16:40-18:20
            new TimeSpan(20, 10, 0),  // Fin del bloque 18:30-20:10
            new TimeSpan(22, 0, 0)    // Fin del bloque 20:20-22:00
        };

        private DateTime? _ultimaEjecucionFinBloque = null;

        public LaboratorioEstadoBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<LaboratorioEstadoBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Servicio de gestión automática de laboratorios iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ActualizarEstadosLaboratoriosPorCronograma();
                    await Task.Delay(_intervalo, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error al actualizar estados de laboratorios");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        private async Task ActualizarEstadosLaboratoriosPorCronograma()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SisComputoDbContext>();

            var horaActual = DateTime.Now;
            var diaSemana = ObtenerDiaSemanaEnEspanol(horaActual.DayOfWeek);
            var horaActualTimeSpan = horaActual.TimeOfDay;

            // ✅ VERIFICAR SI ESTAMOS EN UN HORARIO DE FIN DE BLOQUE
            bool esHorarioFinBloque = EsHorarioDeFinBloque(horaActualTimeSpan);

            _logger.LogInformation("⏰ Verificación - {Hora} - {Dia} | Fin de bloque: {EsFinBloque}", 
                horaActual.ToString("HH:mm:ss"), 
                diaSemana,
                esHorarioFinBloque ? "SÍ" : "NO");

            var laboratorios = await context.Laboratorios.ToListAsync();
            int laboratoriosActualizados = 0;
            int asistenciasFinalizadas = 0;

            foreach (var laboratorio in laboratorios)
            {
                // 1. Verificar si hay un cronograma activo en este momento
                var cronogramaActivo = await context.CronogramaIntervals
                    .Where(c => c.LaboratorioId == laboratorio.LaboratorioId
                        && c.DiaSemana.ToLower() == diaSemana.ToLower()
                        && c.HoraInicio <= horaActualTimeSpan
                        && c.HoraFin >= horaActualTimeSpan
                        && !string.IsNullOrWhiteSpace(c.Materia))
                    .FirstOrDefaultAsync();

                // 2. Obtener todas las asistencias activas
                var asistenciasActivas = await context.Asistencias
                    .Include(a => a.Maquina)
                    .Include(a => a.Usuario)
                    .Include(a => a.Cronograma)
                    .Where(a => a.LaboratorioId == laboratorio.LaboratorioId 
                        && a.HoraSalida == null)
                    .ToListAsync();

                string nuevoEstado;

                // 3. ✅ LÓGICA PRINCIPAL: Determinar el estado según el cronograma
                if (cronogramaActivo != null)
                {
                    // ✅ HAY CLASE PROGRAMADA ACTIVA
                    nuevoEstado = "ocupado";

                    // 🔄 SI ESTAMOS EN HORARIO DE FIN DE BLOQUE, FINALIZAR TODAS LAS ASISTENCIAS
                    if (esHorarioFinBloque && asistenciasActivas.Any())
                    {
                        foreach (var asistencia in asistenciasActivas)
                        {
                            FinalizarAsistenciaInterna(asistencia, horaActual);
                            asistenciasFinalizadas++;

                            _logger.LogInformation(
                                "🔚 Fin de bloque - Asistencia finalizada - Usuario: {Usuario} ({Rol}) | Lab: {Lab} | Máquina: {Maquina} | Materia: {Materia}",
                                $"{asistencia.Usuario?.Nombre} {asistencia.Usuario?.ApellidoPaterno}",
                                asistencia.RolRegistro,
                                laboratorio.CodigoLaboratorio,
                                asistencia.Maquina?.CodigoMaquina ?? "N/A",
                                asistencia.Cronograma?.Materia ?? "N/A");
                        }
                    }
                    // 🔄 SI NO ES FIN DE BLOQUE, SOLO FINALIZAR LAS DE OTROS CRONOGRAMAS
                    else
                    {
                        var asistenciasDeOtrosCronogramas = asistenciasActivas
                            .Where(a => a.CronogramaId != cronogramaActivo.CronogramaId)
                            .ToList();

                        if (asistenciasDeOtrosCronogramas.Any())
                        {
                            foreach (var asistencia in asistenciasDeOtrosCronogramas)
                            {
                                FinalizarAsistenciaInterna(asistencia, horaActual);
                                asistenciasFinalizadas++;

                                _logger.LogInformation(
                                    "✅ Cambio de cronograma - Usuario: {Usuario} ({Rol}) | Lab: {Lab} | Máquina: {Maquina} | {CronogramaAnterior} → {CronogramaNuevo}",
                                    $"{asistencia.Usuario?.Nombre} {asistencia.Usuario?.ApellidoPaterno}",
                                    asistencia.RolRegistro,
                                    laboratorio.CodigoLaboratorio,
                                    asistencia.Maquina?.CodigoMaquina ?? "N/A",
                                    asistencia.Cronograma?.Materia ?? "N/A",
                                    cronogramaActivo.Materia);
                            }
                        }
                    }
                }
                else
                {
                    // ❌ NO HAY CLASE PROGRAMADA
                    nuevoEstado = "libre";
                    
                    // 🔄 FINALIZAR TODAS LAS ASISTENCIAS ACTIVAS
                    if (asistenciasActivas.Any())
                    {
                        foreach (var asistencia in asistenciasActivas)
                        {
                            FinalizarAsistenciaInterna(asistencia, horaActual);
                            asistenciasFinalizadas++;

                            _logger.LogInformation(
                                "✅ Sin cronograma - Usuario: {Usuario} ({Rol}) | Lab: {Lab} | Máquina: {Maquina} | Materia: {Materia}",
                                $"{asistencia.Usuario?.Nombre} {asistencia.Usuario?.ApellidoPaterno}",
                                asistencia.RolRegistro,
                                laboratorio.CodigoLaboratorio,
                                asistencia.Maquina?.CodigoMaquina ?? "N/A",
                                asistencia.Cronograma?.Materia ?? "N/A");
                        }

                        _logger.LogWarning(
                            "⚠️ Laboratorio {CodigoLab} sin cronograma. Finalizadas {Count} asistencias.",
                            laboratorio.CodigoLaboratorio,
                            asistenciasActivas.Count);
                    }
                }

                // 4. Actualizar el estado del laboratorio si cambió
                if (laboratorio.Estado != nuevoEstado)
                {
                    var estadoAnterior = laboratorio.Estado;
                    laboratorio.Estado = nuevoEstado;
                    laboratoriosActualizados++;
                    
                    _logger.LogInformation(
                        "🔄 Laboratorio {CodigoLab}: {EstadoAnterior} → {EstadoNuevo} | Cronograma: {TieneCronograma}",
                        laboratorio.CodigoLaboratorio,
                        estadoAnterior,
                        nuevoEstado,
                        cronogramaActivo != null ? $"{cronogramaActivo.Materia}" : "Ninguno");
                }
            }

            // ✅ GUARDAR TODOS LOS CAMBIOS
            await context.SaveChangesAsync();
            
            _logger.LogInformation(
                "✅ Verificación completada - Labs: {Total} | Actualizados: {Actualizados} | Asistencias finalizadas: {Finalizadas}", 
                laboratorios.Count, 
                laboratoriosActualizados,
                asistenciasFinalizadas);
        }

        /// <summary>
        /// ✅ Verifica si la hora actual corresponde a un horario de fin de bloque
        /// Permite un margen de ±2 minutos para no perder la ejecución
        /// </summary>
        private bool EsHorarioDeFinBloque(TimeSpan horaActual)
        {
            const int margenMinutos = 2;

            foreach (var horarioFin in _horariosFinBloque)
            {
                var diferencia = Math.Abs((horaActual - horarioFin).TotalMinutes);
                
                if (diferencia <= margenMinutos)
                {
                    // Verificar que no se haya ejecutado en los últimos 5 minutos (evitar duplicados)
                    if (_ultimaEjecucionFinBloque == null || 
                        (DateTime.Now - _ultimaEjecucionFinBloque.Value).TotalMinutes > 5)
                    {
                        _ultimaEjecucionFinBloque = DateTime.Now;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// ✅ Finaliza una asistencia (replica del endpoint)
        /// </summary>
        private void FinalizarAsistenciaInterna(Asistencia asistencia, DateTime horaSalida)
        {
            // 1. Registrar hora de salida
            asistencia.HoraSalida = horaSalida;

            // 2. Cambiar máquina a "disponible" (solo si no está en mantenimiento)
            if (asistencia.Maquina != null)
            {
                if (asistencia.Maquina.Estado.ToLower() != "mantenimiento")
                {
                    asistencia.Maquina.Estado = "disponible";
                    
                    _logger.LogDebug(
                        "🖥️ Máquina {CodigoMaquina} → DISPONIBLE",
                        asistencia.Maquina.CodigoMaquina);
                }
                else
                {
                    _logger.LogDebug(
                        "🔧 Máquina {CodigoMaquina} permanece en MANTENIMIENTO",
                        asistencia.Maquina.CodigoMaquina);
                }
            }
        }

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