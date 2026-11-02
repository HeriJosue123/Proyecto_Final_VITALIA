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
                    id_medicamento = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    nombre = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    presentacion = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    estado = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MEDICAMENTO", x => x.id_medicamento);
                });

            migrationBuilder.CreateTable(
                name: "LOTE_MEDICAMENTO",
                columns: table => new
                {
                    id_lote = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    id_medicamento = table.Column<int>(type: "int", nullable: false),
                    numero_lote = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    fecha_vencimiento = table.Column<DateTime>(type: "date", nullable: false),
                    stock = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LOTE_MEDICAMENTO", x => x.id_lote);
                    table.ForeignKey(name: "FK_LOTE_MEDICAMENTO", column: x => x.id_medicamento, principalTable: "MEDICAMENTO", principalColumn: "id_medicamento");
                });

            migrationBuilder.CreateTable(
                name: "ENTREGA_MEDICAMENTO",
                columns: table => new
                {
                    id_entrega = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    id_lote = table.Column<int>(type: "int", nullable: false),
                    id_residente = table.Column<int>(type: "int", nullable: false),
                    id_empleado = table.Column<int>(type: "int", nullable: false),
                    cantidad = table.Column<int>(type: "int", nullable: false),
                    fecha_hora = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    observaciones = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ENTREGA_MEDICAMENTO", x => x.id_entrega);
                    table.ForeignKey(name: "FK_ENTREGA_EMPLEADO", column: x => x.id_empleado, principalTable: "EMPLEADO", principalColumn: "id_empleado");
                    table.ForeignKey(name: "FK_ENTREGA_LOTE", column: x => x.id_lote, principalTable: "LOTE_MEDICAMENTO", principalColumn: "id_lote");
                    table.ForeignKey(name: "FK_ENTREGA_RESIDENTE", column: x => x.id_residente, principalTable: "RESIDENTE", principalColumn: "id_residente");
                });

            migrationBuilder.CreateIndex(name: "IX_ENTREGA_MEDICAMENTO_id_empleado", table: "ENTREGA_MEDICAMENTO", column: "id_empleado");
            migrationBuilder.CreateIndex(name: "IX_ENTREGA_MEDICAMENTO_id_lote", table: "ENTREGA_MEDICAMENTO", column: "id_lote");
            migrationBuilder.CreateIndex(name: "IX_ENTREGA_MEDICAMENTO_id_residente", table: "ENTREGA_MEDICAMENTO", column: "id_residente");
            migrationBuilder.CreateIndex(name: "IX_LOTE_MEDICAMENTO_id_medicamento", table: "LOTE_MEDICAMENTO", column: "id_medicamento");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ENTREGA_MEDICAMENTO");
            migrationBuilder.DropTable(name: "LOTE_MEDICAMENTO");
            migrationBuilder.DropTable(name: "MEDICAMENTO");
        }
    }
}