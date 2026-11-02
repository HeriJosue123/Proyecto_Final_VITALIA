using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VITALIA.Migrations
{
    /// <inheritdoc />
    public partial class AgregarModuloFarmacia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MEDICAMENTO",
                columns: table => new
                {
                    id_medicamento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    nombre = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    presentacion = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    estado = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                        .Annotation("Relational:DefaultConstraintName", "DF_MEDICAMENTO_estado")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MEDICAMENTO", x => x.id_medicamento);
                });

            migrationBuilder.CreateTable(
                name: "RESIDENTE",
                columns: table => new
                {
                    id_residente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codigo_unico = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    apellido = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    fecha_nacimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    num_habitacion = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    tipo_sangre = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    alergias = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    contacto_emergencia = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    observaciones = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    estado = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                        .Annotation("Relational:DefaultConstraintName", "DF_RESIDENTE_estado")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RESIDENTE", x => x.id_residente);
                });

            migrationBuilder.CreateTable(
                name: "ROL",
                columns: table => new
                {
                    id_rol = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre_rol = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROL", x => x.id_rol);
                });

            migrationBuilder.CreateTable(
                name: "TIPO_ATENCION",
                columns: table => new
                {
                    id_tipo_atencion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre_categoria = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TIPO_ATENCION", x => x.id_tipo_atencion);
                });

            migrationBuilder.CreateTable(
                name: "LOTE_MEDICAMENTO",
                columns: table => new
                {
                    id_lote = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_medicamento = table.Column<int>(type: "int", nullable: false),
                    numero_lote = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    fecha_vencimiento = table.Column<DateTime>(type: "date", nullable: false),
                    stock = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LOTE_MEDICAMENTO", x => x.id_lote);
                    table.ForeignKey(
                        name: "FK_LOTE_MEDICAMENTO",
                        column: x => x.id_medicamento,
                        principalTable: "MEDICAMENTO",
                        principalColumn: "id_medicamento");
                });

            migrationBuilder.CreateTable(
                name: "USUARIO",
                columns: table => new
                {
                    id_usuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_rol = table.Column<int>(type: "int", nullable: false),
                    correo = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    contrasena_hash = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    estado = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                        .Annotation("Relational:DefaultConstraintName", "DF_USUARIO_estado")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USUARIO", x => x.id_usuario);
                    table.ForeignKey(
                        name: "FK_USUARIO_ROL",
                        column: x => x.id_rol,
                        principalTable: "ROL",
                        principalColumn: "id_rol");
                });

            migrationBuilder.CreateTable(
                name: "EMPLEADO",
                columns: table => new
                {
                    id_empleado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    apellido = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    cargo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    estado = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                        .Annotation("Relational:DefaultConstraintName", "DF_EMPLEADO_estado")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMPLEADO", x => x.id_empleado);
                    table.ForeignKey(
                        name: "FK_EMPLEADO_USUARIO",
                        column: x => x.id_usuario,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario");
                });

            migrationBuilder.CreateTable(
                name: "FAMILIAR_RESIDENTE",
                columns: table => new
                {
                    id_familiar_residente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_residente = table.Column<int>(type: "int", nullable: false),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    parentesco = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    telefono_contacto = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FAMILIAR_RESIDENTE", x => x.id_familiar_residente);
                    table.ForeignKey(
                        name: "FK_FAMILIAR_RESIDENTE_RESIDENTE",
                        column: x => x.id_residente,
                        principalTable: "RESIDENTE",
                        principalColumn: "id_residente");
                    table.ForeignKey(
                        name: "FK_FAMILIAR_RESIDENTE_USUARIO",
                        column: x => x.id_usuario,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario");
                });

            migrationBuilder.CreateTable(
                name: "ALERTA",
                columns: table => new
                {
                    id_alerta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_residente = table.Column<int>(type: "int", nullable: false),
                    id_usuario_creador = table.Column<int>(type: "int", nullable: false),
                    titulo = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false),
                    fecha_programada = table.Column<DateTime>(type: "datetime", nullable: false),
                    estado_actual = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "Pendiente")
                        .Annotation("Relational:DefaultConstraintName", "DF_ALERTA_estado"),
                    prioridad = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Rutina")
                        .Annotation("Relational:DefaultConstraintName", "DF_ALERTA_prioridad"),
                    id_empleado_responsable = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ALERTA", x => x.id_alerta);
                    table.CheckConstraint("CK_Alerta_Estado", "[estado_actual] IN ('Pendiente', 'En Proceso', 'Atendida')");
                    table.ForeignKey(
                        name: "FK_ALERTA_EMPLEADO",
                        column: x => x.id_empleado_responsable,
                        principalTable: "EMPLEADO",
                        principalColumn: "id_empleado");
                    table.ForeignKey(
                        name: "FK_ALERTA_RESIDENTE",
                        column: x => x.id_residente,
                        principalTable: "RESIDENTE",
                        principalColumn: "id_residente");
                    table.ForeignKey(
                        name: "FK_ALERTA_USUARIO",
                        column: x => x.id_usuario_creador,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario");
                });

            migrationBuilder.CreateTable(
                name: "ASIGNACION_CUIDADO",
                columns: table => new
                {
                    id_asignacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_empleado = table.Column<int>(type: "int", nullable: false),
                    id_residente = table.Column<int>(type: "int", nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_ASIGNACION_inicio"),
                    fecha_fin = table.Column<DateTime>(type: "datetime", nullable: true),
                    estado = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                        .Annotation("Relational:DefaultConstraintName", "DF_ASIGNACION_estado")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASIGNACION_CUIDADO", x => x.id_asignacion);
                    table.ForeignKey(
                        name: "FK_ASIGNACION_EMPLEADO",
                        column: x => x.id_empleado,
                        principalTable: "EMPLEADO",
                        principalColumn: "id_empleado");
                    table.ForeignKey(
                        name: "FK_ASIGNACION_RESIDENTE",
                        column: x => x.id_residente,
                        principalTable: "RESIDENTE",
                        principalColumn: "id_residente");
                });

            migrationBuilder.CreateTable(
                name: "ATENCION_DIARIA",
                columns: table => new
                {
                    id_atencion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_residente = table.Column<int>(type: "int", nullable: false),
                    id_empleado = table.Column<int>(type: "int", nullable: false),
                    id_tipo_atencion = table.Column<int>(type: "int", nullable: false),
                    fecha_hora = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_ATENCION_fecha"),
                    observaciones = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false),
                    estado_cumplimiento = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, defaultValue: "Completado")
                        .Annotation("Relational:DefaultConstraintName", "DF_ATENCION_cumplimiento")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ATENCION_DIARIA", x => x.id_atencion);
                    table.ForeignKey(
                        name: "FK_ATENCION_EMPLEADO",
                        column: x => x.id_empleado,
                        principalTable: "EMPLEADO",
                        principalColumn: "id_empleado");
                    table.ForeignKey(
                        name: "FK_ATENCION_RESIDENTE",
                        column: x => x.id_residente,
                        principalTable: "RESIDENTE",
                        principalColumn: "id_residente");
                    table.ForeignKey(
                        name: "FK_ATENCION_TIPO",
                        column: x => x.id_tipo_atencion,
                        principalTable: "TIPO_ATENCION",
                        principalColumn: "id_tipo_atencion");
                });

            migrationBuilder.CreateTable(
                name: "ENTREGA_MEDICAMENTO",
                columns: table => new
                {
                    id_entrega = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_lote = table.Column<int>(type: "int", nullable: false),
                    id_residente = table.Column<int>(type: "int", nullable: false),
                    id_empleado = table.Column<int>(type: "int", nullable: false),
                    cantidad = table.Column<int>(type: "int", nullable: false),
                    fecha_hora = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    observaciones = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    MedicamentoIdMedicamento = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ENTREGA_MEDICAMENTO", x => x.id_entrega);
                    table.ForeignKey(
                        name: "FK_ENTREGA_EMPLEADO",
                        column: x => x.id_empleado,
                        principalTable: "EMPLEADO",
                        principalColumn: "id_empleado");
                    table.ForeignKey(
                        name: "FK_ENTREGA_LOTE",
                        column: x => x.id_lote,
                        principalTable: "LOTE_MEDICAMENTO",
                        principalColumn: "id_lote");
                    table.ForeignKey(
                        name: "FK_ENTREGA_MEDICAMENTO_MEDICAMENTO_MedicamentoIdMedicamento",
                        column: x => x.MedicamentoIdMedicamento,
                        principalTable: "MEDICAMENTO",
                        principalColumn: "id_medicamento");
                    table.ForeignKey(
                        name: "FK_ENTREGA_RESIDENTE",
                        column: x => x.id_residente,
                        principalTable: "RESIDENTE",
                        principalColumn: "id_residente");
                });

            migrationBuilder.CreateTable(
                name: "HISTORIAL_ESTADO_ALERTA",
                columns: table => new
                {
                    id_historial_estado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_alerta = table.Column<int>(type: "int", nullable: false),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    estado_anterior = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    estado_nuevo = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    fecha_hora = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_HISTORIAL_ALERTA_fecha"),
                    comentario = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HISTORIAL_ESTADO_ALERTA", x => x.id_historial_estado);
                    table.ForeignKey(
                        name: "FK_HISTORIAL_ALERTA",
                        column: x => x.id_alerta,
                        principalTable: "ALERTA",
                        principalColumn: "id_alerta");
                    table.ForeignKey(
                        name: "FK_HISTORIAL_USUARIO",
                        column: x => x.id_usuario,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario");
                });

            migrationBuilder.CreateTable(
                name: "CORRECCION_ATENCION",
                columns: table => new
                {
                    id_correccion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_atencion = table.Column<int>(type: "int", nullable: false),
                    id_usuario_admin = table.Column<int>(type: "int", nullable: false),
                    motivo = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false),
                    tipo_accion = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    fecha_hora = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_CORRECCION_fecha")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CORRECCION_ATENCION", x => x.id_correccion);
                    table.ForeignKey(
                        name: "FK_CORRECCION_ATENCION",
                        column: x => x.id_atencion,
                        principalTable: "ATENCION_DIARIA",
                        principalColumn: "id_atencion");
                    table.ForeignKey(
                        name: "FK_CORRECCION_USUARIO",
                        column: x => x.id_usuario_admin,
                        principalTable: "USUARIO",
                        principalColumn: "id_usuario");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ALERTA_id_empleado_responsable",
                table: "ALERTA",
                column: "id_empleado_responsable");

            migrationBuilder.CreateIndex(
                name: "IX_ALERTA_id_residente",
                table: "ALERTA",
                column: "id_residente");

            migrationBuilder.CreateIndex(
                name: "IX_ALERTA_id_usuario_creador",
                table: "ALERTA",
                column: "id_usuario_creador");

            migrationBuilder.CreateIndex(
                name: "IX_ASIGNACION_CUIDADO_id_empleado",
                table: "ASIGNACION_CUIDADO",
                column: "id_empleado");

            migrationBuilder.CreateIndex(
                name: "IX_ASIGNACION_CUIDADO_id_residente",
                table: "ASIGNACION_CUIDADO",
                column: "id_residente");

            migrationBuilder.CreateIndex(
                name: "IX_ATENCION_DIARIA_id_empleado",
                table: "ATENCION_DIARIA",
                column: "id_empleado");

            migrationBuilder.CreateIndex(
                name: "IX_ATENCION_DIARIA_id_residente",
                table: "ATENCION_DIARIA",
                column: "id_residente");

            migrationBuilder.CreateIndex(
                name: "IX_ATENCION_DIARIA_id_tipo_atencion",
                table: "ATENCION_DIARIA",
                column: "id_tipo_atencion");

            migrationBuilder.CreateIndex(
                name: "IX_CORRECCION_ATENCION_id_atencion",
                table: "CORRECCION_ATENCION",
                column: "id_atencion");

            migrationBuilder.CreateIndex(
                name: "IX_CORRECCION_ATENCION_id_usuario_admin",
                table: "CORRECCION_ATENCION",
                column: "id_usuario_admin");

            migrationBuilder.CreateIndex(
                name: "UQ_EMPLEADO_usuario",
                table: "EMPLEADO",
                column: "id_usuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ENTREGA_MEDICAMENTO_id_empleado",
                table: "ENTREGA_MEDICAMENTO",
                column: "id_empleado");

            migrationBuilder.CreateIndex(
                name: "IX_ENTREGA_MEDICAMENTO_id_lote",
                table: "ENTREGA_MEDICAMENTO",
                column: "id_lote");

            migrationBuilder.CreateIndex(
                name: "IX_ENTREGA_MEDICAMENTO_id_residente",
                table: "ENTREGA_MEDICAMENTO",
                column: "id_residente");

            migrationBuilder.CreateIndex(
                name: "IX_ENTREGA_MEDICAMENTO_MedicamentoIdMedicamento",
                table: "ENTREGA_MEDICAMENTO",
                column: "MedicamentoIdMedicamento");

            migrationBuilder.CreateIndex(
                name: "IX_FAMILIAR_RESIDENTE_id_residente",
                table: "FAMILIAR_RESIDENTE",
                column: "id_residente");

            migrationBuilder.CreateIndex(
                name: "IX_FAMILIAR_RESIDENTE_id_usuario",
                table: "FAMILIAR_RESIDENTE",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_HISTORIAL_ESTADO_ALERTA_id_alerta",
                table: "HISTORIAL_ESTADO_ALERTA",
                column: "id_alerta");

            migrationBuilder.CreateIndex(
                name: "IX_HISTORIAL_ESTADO_ALERTA_id_usuario",
                table: "HISTORIAL_ESTADO_ALERTA",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_LOTE_MEDICAMENTO_id_medicamento",
                table: "LOTE_MEDICAMENTO",
                column: "id_medicamento");

            migrationBuilder.CreateIndex(
                name: "UQ_RESIDENTE_codigo",
                table: "RESIDENTE",
                column: "codigo_unico",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_ROL_nombre",
                table: "ROL",
                column: "nombre_rol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_TIPO_ATENCION_nombre",
                table: "TIPO_ATENCION",
                column: "nombre_categoria",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USUARIO_id_rol",
                table: "USUARIO",
                column: "id_rol");

            migrationBuilder.CreateIndex(
                name: "UQ_USUARIO_correo",
                table: "USUARIO",
                column: "correo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ASIGNACION_CUIDADO");

            migrationBuilder.DropTable(
                name: "CORRECCION_ATENCION");

            migrationBuilder.DropTable(
                name: "ENTREGA_MEDICAMENTO");

            migrationBuilder.DropTable(
                name: "FAMILIAR_RESIDENTE");

            migrationBuilder.DropTable(
                name: "HISTORIAL_ESTADO_ALERTA");

            migrationBuilder.DropTable(
                name: "ATENCION_DIARIA");

            migrationBuilder.DropTable(
                name: "LOTE_MEDICAMENTO");

            migrationBuilder.DropTable(
                name: "ALERTA");

            migrationBuilder.DropTable(
                name: "TIPO_ATENCION");

            migrationBuilder.DropTable(
                name: "MEDICAMENTO");

            migrationBuilder.DropTable(
                name: "EMPLEADO");

            migrationBuilder.DropTable(
                name: "RESIDENTE");

            migrationBuilder.DropTable(
                name: "USUARIO");

            migrationBuilder.DropTable(
                name: "ROL");
        }
    }
}
