using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCLAB_API.Migrations
{
    /// <inheritdoc />
    public partial class IntialCreateUltimo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Laboratorio",
                columns: table => new
                {
                    LaboratorioId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoLaboratorio = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Ubicacion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Capacidad = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Laboratorio", x => x.LaboratorioId);
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApellidoPaterno = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApellidoMaterno = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CorreoInstitucional = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CI = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.UsuarioId);
                });

            migrationBuilder.CreateTable(
                name: "CronogramaInterval",
                columns: table => new
                {
                    CronogramaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LaboratorioId = table.Column<int>(type: "int", nullable: false),
                    DiaSemana = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HoraInicio = table.Column<TimeSpan>(type: "time", nullable: false),
                    HoraFin = table.Column<TimeSpan>(type: "time", nullable: false),
                    Materia = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Observacion = table.Column<string>(type: "NVARCHAR(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CronogramaInterval", x => x.CronogramaId);
                    table.ForeignKey(
                        name: "FK_CronogramaInterval_Laboratorio_LaboratorioId",
                        column: x => x.LaboratorioId,
                        principalTable: "Laboratorio",
                        principalColumn: "LaboratorioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Maquina",
                columns: table => new
                {
                    MaquinaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoMaquina = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LaboratorioId = table.Column<int>(type: "int", nullable: false),
                    DescripcionHardware = table.Column<string>(type: "NVARCHAR(MAX)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Qr = table.Column<byte[]>(type: "VARBINARY(MAX)", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maquina", x => x.MaquinaId);
                    table.ForeignKey(
                        name: "FK_Maquina_Laboratorio_LaboratorioId",
                        column: x => x.LaboratorioId,
                        principalTable: "Laboratorio",
                        principalColumn: "LaboratorioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LogActividad",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Detalle = table.Column<string>(type: "NVARCHAR(MAX)", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogActividad", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_LogActividad_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alerta",
                columns: table => new
                {
                    AlertaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaquinaId = table.Column<int>(type: "int", nullable: false),
                    LaboratorioId = table.Column<int>(type: "int", nullable: true),
                    CreadaPor = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false),
                    EstadoAlerta = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaResolucion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResueltoPor = table.Column<int>(type: "int", nullable: true),
                    SolucionTipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SolucionDescripcion = table.Column<string>(type: "NVARCHAR(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerta", x => x.AlertaId);
                    table.ForeignKey(
                        name: "FK_Alerta_Laboratorio_LaboratorioId",
                        column: x => x.LaboratorioId,
                        principalTable: "Laboratorio",
                        principalColumn: "LaboratorioId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Alerta_Maquina_MaquinaId",
                        column: x => x.MaquinaId,
                        principalTable: "Maquina",
                        principalColumn: "MaquinaId");
                    table.ForeignKey(
                        name: "FK_Alerta_Usuario_CreadaPor",
                        column: x => x.CreadaPor,
                        principalTable: "Usuario",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Alerta_Usuario_ResueltoPor",
                        column: x => x.ResueltoPor,
                        principalTable: "Usuario",
                        principalColumn: "UsuarioId");
                });

            migrationBuilder.CreateTable(
                name: "Asistencia",
                columns: table => new
                {
                    AsistenciaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    MaquinaId = table.Column<int>(type: "int", nullable: false),
                    LaboratorioId = table.Column<int>(type: "int", nullable: false),
                    CronogramaId = table.Column<int>(type: "int", nullable: true),
                    RegistroPor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HoraIngreso = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HoraSalida = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RolRegistro = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Observacion = table.Column<string>(type: "NVARCHAR(MAX)", nullable: true),
                    TipoDispositivo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asistencia", x => x.AsistenciaId);
                    table.ForeignKey(
                        name: "FK_Asistencia_CronogramaInterval_CronogramaId",
                        column: x => x.CronogramaId,
                        principalTable: "CronogramaInterval",
                        principalColumn: "CronogramaId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Asistencia_Laboratorio_LaboratorioId",
                        column: x => x.LaboratorioId,
                        principalTable: "Laboratorio",
                        principalColumn: "LaboratorioId");
                    table.ForeignKey(
                        name: "FK_Asistencia_Maquina_MaquinaId",
                        column: x => x.MaquinaId,
                        principalTable: "Maquina",
                        principalColumn: "MaquinaId");
                    table.ForeignKey(
                        name: "FK_Asistencia_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "UsuarioId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerta_CreadaPor",
                table: "Alerta",
                column: "CreadaPor");

            migrationBuilder.CreateIndex(
                name: "IX_Alerta_EstadoAlerta",
                table: "Alerta",
                column: "EstadoAlerta");

            migrationBuilder.CreateIndex(
                name: "IX_Alerta_LaboratorioId",
                table: "Alerta",
                column: "LaboratorioId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerta_MaquinaId",
                table: "Alerta",
                column: "MaquinaId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerta_ResueltoPor",
                table: "Alerta",
                column: "ResueltoPor");

            migrationBuilder.CreateIndex(
                name: "IX_Asistencia_CronogramaId",
                table: "Asistencia",
                column: "CronogramaId");

            migrationBuilder.CreateIndex(
                name: "IX_Asistencia_LaboratorioId",
                table: "Asistencia",
                column: "LaboratorioId");

            migrationBuilder.CreateIndex(
                name: "IX_Asistencia_MaquinaId",
                table: "Asistencia",
                column: "MaquinaId");

            migrationBuilder.CreateIndex(
                name: "IX_Asistencia_Tipo",
                table: "Asistencia",
                column: "Tipo");

            migrationBuilder.CreateIndex(
                name: "IX_Asistencia_UsuarioId",
                table: "Asistencia",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CronogramaInterval_LaboratorioId_DiaSemana",
                table: "CronogramaInterval",
                columns: new[] { "LaboratorioId", "DiaSemana" });

            migrationBuilder.CreateIndex(
                name: "IX_Laboratorio_CodigoLaboratorio",
                table: "Laboratorio",
                column: "CodigoLaboratorio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogActividad_UsuarioId",
                table: "LogActividad",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Maquina_CodigoMaquina",
                table: "Maquina",
                column: "CodigoMaquina",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Maquina_Estado",
                table: "Maquina",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Maquina_LaboratorioId",
                table: "Maquina",
                column: "LaboratorioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_CI",
                table: "Usuario",
                column: "CI",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_CorreoInstitucional",
                table: "Usuario",
                column: "CorreoInstitucional",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alerta");

            migrationBuilder.DropTable(
                name: "Asistencia");

            migrationBuilder.DropTable(
                name: "LogActividad");

            migrationBuilder.DropTable(
                name: "CronogramaInterval");

            migrationBuilder.DropTable(
                name: "Maquina");

            migrationBuilder.DropTable(
                name: "Usuario");

            migrationBuilder.DropTable(
                name: "Laboratorio");
        }
    }
}
