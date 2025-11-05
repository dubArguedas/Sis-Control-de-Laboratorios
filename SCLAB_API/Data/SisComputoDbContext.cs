using Microsoft.EntityFrameworkCore;
using SCLAB_API.Models;

namespace SCLAB_API.Data
{
    public class SisComputoDbContext : DbContext
    {
        public SisComputoDbContext(DbContextOptions<SisComputoDbContext> options)
            : base(options)
        {
        }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Laboratorio> Laboratorios { get; set; }
        public DbSet<Maquina> Maquinas { get; set; }
        public DbSet<CronogramaInterval> CronogramaIntervals { get; set; }
        public DbSet<Asistencia> Asistencias { get; set; }
        public DbSet<Alerta> Alertas { get; set; }
        public DbSet<LogActividad> LogActividades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Usuario
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasIndex(e => e.CorreoInstitucional).IsUnique();
                entity.HasIndex(e => e.CI).IsUnique();

                entity.HasMany(e => e.AlertasCreadas)
                    .WithOne(e => e.UsuarioCreador)
                    .HasForeignKey(e => e.CreadaPor)
                    .OnDelete(DeleteBehavior.Cascade);

                // SQL Server no permite múltiples cascadas en la misma tabla
                entity.HasMany(e => e.AlertasResueltas)
                    .WithOne(e => e.UsuarioResolutor)
                    .HasForeignKey(e => e.ResueltoPor)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Laboratorio
            modelBuilder.Entity<Laboratorio>(entity =>
            {
                entity.HasIndex(e => e.CodigoLaboratorio).IsUnique();
            });

            // Maquina
            modelBuilder.Entity<Maquina>(entity =>
            {
                entity.HasIndex(e => e.CodigoMaquina).IsUnique();
                entity.HasIndex(e => e.Estado);

                entity.HasOne(e => e.Laboratorio)
                    .WithMany(e => e.Maquinas)
                    .HasForeignKey(e => e.LaboratorioId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // CronogramaInterval
            modelBuilder.Entity<CronogramaInterval>(entity =>
            {
                entity.HasIndex(e => new { e.LaboratorioId, e.DiaSemana });

                entity.HasOne(e => e.Laboratorio)
                    .WithMany(e => e.Cronogramas)
                    .HasForeignKey(e => e.LaboratorioId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Asistencia - SQL Server tiene restricciones con cascadas múltiples
            modelBuilder.Entity<Asistencia>(entity =>
            {
                entity.HasIndex(e => e.Tipo);

                entity.HasOne(e => e.Usuario)
                    .WithMany(e => e.Asistencias)
                    .HasForeignKey(e => e.UsuarioId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Maquina)
                    .WithMany(e => e.Asistencias)
                    .HasForeignKey(e => e.MaquinaId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Laboratorio)
                    .WithMany(e => e.Asistencias)
                    .HasForeignKey(e => e.LaboratorioId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Cronograma)
                    .WithMany(e => e.Asistencias)
                    .HasForeignKey(e => e.CronogramaId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Alerta
            modelBuilder.Entity<Alerta>(entity =>
            {
                entity.HasIndex(e => e.EstadoAlerta);

                entity.HasOne(e => e.Maquina)
                    .WithMany(e => e.Alertas)
                    .HasForeignKey(e => e.MaquinaId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Laboratorio)
                    .WithMany(e => e.Alertas)
                    .HasForeignKey(e => e.LaboratorioId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // LogActividad
            modelBuilder.Entity<LogActividad>(entity =>
            {
                entity.HasOne(e => e.Usuario)
                    .WithMany(e => e.LogActividades)
                    .HasForeignKey(e => e.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
