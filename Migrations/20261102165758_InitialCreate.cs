using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VITALIA.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ROL",
                columns: table => new
                {
                    id_rol = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
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
                    id_tipo_atencion = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    nombre_categoria = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TIPO_ATENCION", x => x.id_tipo_atencion);
                });

            migrationBuilder.CreateTable(
                name: "RESIDENTE",
                columns: table => new
                {
                    id_residente = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
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
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RESIDENTE", x => x.id_residente);
                });

            migrationBuilder.CreateTable(
                name: "USUARIO",
                columns: table => new
                {
                    id_usuario = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    id_rol = table.Column<int>(type: "int", nullable: false),
                    correo = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    contrasena_hash = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    estado = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USUARIO", x => x.id_usuario);
                    table.ForeignKey(name: "FK_USUARIO_ROL", column: x => x.id_rol, principalTable: "ROL", principalColumn: "id_rol");
                });

            migrationBuilder.CreateTable(
                name: "EMPLEADO",
                columns: table => new
                {
                    id_empleado = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    apellido = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    cargo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    estado = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMPLEADO", x => x.id_empleado);
                    table.ForeignKey(name: "FK_EMPLEADO_USUARIO", column: x => x.id_usuario, principalTable: "USUARIO", principalColumn: "id_usuario");
                });

            migrationBuilder.CreateTable(
                name: "FAMILIAR_RESIDENTE",
                columns: table => new
                {
                    id_familiar_residente = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    id_residente = table.Column<int>(type: "int", nullable: false),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    parentesco = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    telefono_contacto = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FAMILIAR_RESIDENTE", x => x.id_familiar_residente);
                    table.ForeignKey(name: "FK_FAMILIAR_RESIDENTE_RESIDENTE", column: x => x.id_residente, principalTable: "RESIDENTE", principalColumn: "id_residente");
                    table.ForeignKey(name: "FK_FAMILIAR_RESIDENTE_USUARIO", column: x => x.id_usuario, principalTable: "USUARIO", principalColumn: "id_usuario");
                });

            migrationBuilder.CreateTable(
                name: "ASIGNACION_CUIDADO",
                columns: table => new
                {
                    id_asignacion = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    id_empleado = table.Column<int>(type: "int", nullable: false),
                    id_residente = table.Column<int>(type: "int", nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    fecha_fin = table.Column<DateTime>(type: "datetime", nullable: true),
                    estado = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASIGNACION_CUIDADO", x => x.id_asignacion);
                    table.ForeignKey(name: "FK_ASIGNACION_EMPLEADO", column: x => x.id_empleado, principalTable: "EMPLEADO", principalColumn: "id_empleado");
                    table.ForeignKey(name: "FK_ASIGNACION_RESIDENTE", column: x => x.id_residente, principalTable: "RESIDENTE", principalColumn: "id_residente");
                });

            migrationBuilder.CreateTable(
                name: "ATENCION_DIARIA",
                columns: table => new
                {
                    id_atencion = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    id_residente = table.Column<int>(type: "int", nullable: false),
                    id_empleado = table.Column<int>(type: "int", nullable: false),
                    id_tipo_atencion = table.Column<int>(type: "int", nullable: false),
                    fecha_hora = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    observaciones = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false),
                    estado_cumplimiento = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, defaultValue: "Completado")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ATENCION_DIARIA", x => x.id_atencion);
                    table.ForeignKey(name: "FK_ATENCION_EMPLEADO", column: x => x.id_empleado, principalTable: "EMPLEADO", principalColumn: "id_empleado");
                    table.ForeignKey(name: "FK_ATENCION_RESIDENTE", column: x => x.id_residente, principalTable: "RESIDENTE", principalColumn: "id_residente");
                    table.ForeignKey(name: "FK_ATENCION_TIPO", column: x => x.id_tipo_atencion, principalTable: "TIPO_ATENCION", principalColumn: "id_tipo_atencion");
                });

            migrationBuilder.CreateTable(
                name: "ALERTA",
                columns: table => new
                {
                    id_alerta = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    id_residente = table.Column<int>(type: "int", nullable: false),
                    id_usuario_creador = table.Column<int>(type: "int", nullable: false),
                    titulo = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false),
                    fecha_programada = table.Column<DateTime>(type: "datetime", nullable: false),
                    estado_actual = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "Pendiente"),
                    prioridad = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Rutina"),
                    id_empleado_responsable = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>