using Microsoft.EntityFrameworkCore;
using SCLAB_API.Data;

namespace SCLAB_API.Services
{
    public class LaboratorioEstadoBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LaboratorioEstadoBackgroundService> _logger;
        private readonly TimeSpan _intervalo = TimeSpan.FromMinutes(1); // Revisar cada 1 minuto

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

            _logger.LogInformation("⏰ Ejecutando verificación de laboratorios - {Hora} - {Dia}", 
                horaActual.ToString("HH:mm:ss"), diaSemana);

            var laboratorios = await context.Laboratorios.ToListAsync();
            int laboratoriosActualizados = 0;

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

                // 2. Contar asistencias activas del laboratorio
                var asistenciasActivas = await context.Asistencias
                    .Where(a => a.LaboratorioId == laboratorio.LaboratorioId 
                        && a.HoraSalida == null)
                    .CountAsync();

                string nuevoEstado;

                // 3. ✅ LÓGICA CORREGIDA: Determinar el estado según el cronograma
                if (cronogramaActivo != null)
                {
                    // ✅ HAY CLASE PROGRAMADA: El laboratorio está OCUPADO (independiente de las asistencias)
                    nuevoEstado = "ocupado";
                }
                else
                {
                    // NO HAY CLASE PROGRAMADA
                    if (asistenciasActivas > 0)
                    {
                        // Si hay asistencias activas sin cronograma, finalizarlas automáticamente
                        await FinalizarAsistenciasHuerfanas(context, laboratorio.LaboratorioId, horaActual);
                        
                        _logger.LogWarning(
                            "⚠️ Laboratorio {CodigoLab} tenía {Count} asistencias activas sin cronograma vigente. Se finalizaron automáticamente.",
                            laboratorio.CodigoLaboratorio,
                            asistenciasActivas);
                    }
                    
                    // El laboratorio debe estar libre
                    nuevoEstado = "libre";
                }

                // 4. Actualizar el estado si cambió
                if (laboratorio.Estado != nuevoEstado)
                {
                    var estadoAnterior = laboratorio.Estado;
                    laboratorio.Estado = nuevoEstado;
                    laboratoriosActualizados++;
                    
                    _logger.LogInformation(
                        "🔄 Laboratorio {CodigoLab}: {EstadoAnterior} → {EstadoNuevo} | Cronograma: {TieneCronograma} | Asistencias: {Count}",
                        laboratorio.CodigoLaboratorio,
                        estadoAnterior,
                        nuevoEstado,
                        cronogramaActivo != null ? $"{cronogramaActivo.Materia}" : "Ninguno",
                        asistenciasActivas);
                }
            }

            await context.SaveChangesAsync();
            
            _logger.LogInformation("✅ Verificación completada - Laboratorios revisados: {Total} - Actualizados: {Actualizados}", 
                laboratorios.Count, laboratoriosActualizados);
        }

        private async Task FinalizarAsistenciasHuerfanas(SisComputoDbContext context, int laboratorioId, DateTime horaSalida)
        {
            var asistenciasHuerfanas = await context.Asistencias
                .Include(a => a.Maquina)
                .Where(a => a.LaboratorioId == laboratorioId && a.HoraSalida == null)
                .ToListAsync();

            foreach (var asistencia in asistenciasHuerfanas)
            {
                asistencia.HoraSalida = horaSalida;
                
                // Liberar la máquina si no está en mantenimiento
                if (asistencia.Maquina != null && asistencia.Maquina.Estado.ToLower() != "mantenimiento")
                {
                    asistencia.Maquina.Estado = "libre";
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