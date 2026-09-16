using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InvestFlow.Infrastructure.Persistence.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ativos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ticker = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    PrecoAtual = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ativos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ordens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AtivoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Lado = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantidade = table.Column<int>(type: "INTEGER", nullable: false),
                    PrecoExecucao = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    DataExecucao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ordens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ordens_Ativos_AtivoId",
                        column: x => x.AtivoId,
                        principalTable: "Ativos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Ativos",
                columns: new[] { "Id", "CriadoEm", "Nome", "PrecoAtual", "Ticker", "Tipo" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 2, 10, 0, 0, 0, DateTimeKind.Utc), "Petrobras PN", 38.1200m, "PETR4", 1 },
                    { 2, new DateTime(2026, 1, 2, 10, 0, 0, 0, DateTimeKind.Utc), "Vale ON", 61.4500m, "VALE3", 1 },
                    { 3, new DateTime(2026, 1, 2, 10, 0, 0, 0, DateTimeKind.Utc), "CSHG Logística FII", 162.3500m, "HGLG11", 2 },
                    { 4, new DateTime(2026, 1, 2, 10, 0, 0, 0, DateTimeKind.Utc), "Tesouro IPCA+ 2035", 2150.7800m, "IPCA2035", 3 },
                    { 5, new DateTime(2026, 1, 2, 10, 0, 0, 0, DateTimeKind.Utc), "Mini Índice Futuro Dez/26", 131250.0000m, "WINZ26", 4 }
                });

            migrationBuilder.InsertData(
                table: "Ordens",
                columns: new[] { "Id", "AtivoId", "DataExecucao", "Lado", "PrecoExecucao", "Quantidade", "Status" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 1, 4, 10, 17, 0, 0, DateTimeKind.Utc), 1, 38.3487m, 200, 2 },
                    { 2, 2, new DateTime(2026, 1, 6, 10, 34, 0, 0, DateTimeKind.Utc), 1, 61.5114m, 300, 2 },
                    { 3, 3, new DateTime(2026, 1, 8, 10, 51, 0, 0, DateTimeKind.Utc), 2, 161.7006m, 40, 2 },
                    { 4, 4, new DateTime(2026, 1, 10, 11, 8, 0, 0, DateTimeKind.Utc), 1, 2131.4230m, 5, 2 },
                    { 5, 5, new DateTime(2026, 1, 12, 11, 25, 0, 0, DateTimeKind.Utc), 1, 132168.7500m, 6, 2 },
                    { 6, 1, new DateTime(2026, 1, 14, 11, 42, 0, 0, DateTimeKind.Utc), 2, 38.1962m, 700, 2 },
                    { 7, 2, new DateTime(2026, 1, 16, 11, 59, 0, 0, DateTimeKind.Utc), 1, 61.2656m, 100, 2 },
                    { 8, 3, new DateTime(2026, 1, 18, 12, 16, 0, 0, DateTimeKind.Utc), 1, 161.0512m, 20, 2 },
                    { 9, 4, new DateTime(2026, 1, 20, 12, 33, 0, 0, DateTimeKind.Utc), 2, 2167.9862m, 3, 2 },
                    { 10, 5, new DateTime(2026, 1, 22, 12, 50, 0, 0, DateTimeKind.Utc), 1, 131643.7500m, 4, 3 },
                    { 11, 1, new DateTime(2026, 1, 24, 13, 7, 0, 0, DateTimeKind.Utc), 1, 38.0438m, 500, 2 },
                    { 12, 2, new DateTime(2026, 1, 26, 13, 24, 0, 0, DateTimeKind.Utc), 2, 61.0198m, 600, 2 },
                    { 13, 3, new DateTime(2026, 1, 28, 13, 41, 0, 0, DateTimeKind.Utc), 1, 163.8112m, 70, 2 },
                    { 14, 4, new DateTime(2026, 1, 30, 13, 58, 0, 0, DateTimeKind.Utc), 1, 2159.3831m, 1, 2 },
                    { 15, 5, new DateTime(2026, 2, 1, 14, 15, 0, 0, DateTimeKind.Utc), 2, 131118.7500m, 2, 2 },
                    { 16, 1, new DateTime(2026, 2, 3, 14, 32, 0, 0, DateTimeKind.Utc), 1, 37.8913m, 300, 2 },
                    { 17, 2, new DateTime(2026, 2, 5, 14, 49, 0, 0, DateTimeKind.Utc), 1, 62.0645m, 400, 2 },
                    { 18, 3, new DateTime(2026, 2, 7, 15, 6, 0, 0, DateTimeKind.Utc), 2, 163.1618m, 50, 2 },
                    { 19, 4, new DateTime(2026, 2, 9, 15, 23, 0, 0, DateTimeKind.Utc), 1, 2150.7800m, 6, 2 },
                    { 20, 5, new DateTime(2026, 2, 11, 15, 40, 0, 0, DateTimeKind.Utc), 1, 130593.7500m, 7, 3 },
                    { 21, 1, new DateTime(2026, 2, 13, 15, 57, 0, 0, DateTimeKind.Utc), 2, 37.7388m, 100, 2 },
                    { 22, 2, new DateTime(2026, 2, 15, 16, 14, 0, 0, DateTimeKind.Utc), 1, 61.8187m, 200, 2 },
                    { 23, 3, new DateTime(2026, 2, 17, 16, 31, 0, 0, DateTimeKind.Utc), 1, 162.5124m, 30, 2 },
                    { 24, 4, new DateTime(2026, 2, 19, 16, 48, 0, 0, DateTimeKind.Utc), 2, 2142.1769m, 4, 2 },
                    { 25, 5, new DateTime(2026, 2, 21, 17, 5, 0, 0, DateTimeKind.Utc), 1, 130068.7500m, 5, 2 },
                    { 26, 1, new DateTime(2026, 2, 23, 17, 22, 0, 0, DateTimeKind.Utc), 1, 38.3868m, 600, 2 },
                    { 27, 2, new DateTime(2026, 2, 25, 17, 39, 0, 0, DateTimeKind.Utc), 2, 61.5729m, 700, 2 },
                    { 28, 3, new DateTime(2026, 2, 27, 17, 56, 0, 0, DateTimeKind.Utc), 1, 161.8630m, 10, 2 },
                    { 29, 4, new DateTime(2026, 3, 1, 18, 13, 0, 0, DateTimeKind.Utc), 1, 2133.5738m, 2, 2 },
                    { 30, 5, new DateTime(2026, 3, 3, 18, 30, 0, 0, DateTimeKind.Utc), 2, 132300.0000m, 3, 3 },
                    { 31, 1, new DateTime(2026, 3, 5, 18, 47, 0, 0, DateTimeKind.Utc), 1, 38.2344m, 400, 2 },
                    { 32, 2, new DateTime(2026, 3, 7, 19, 4, 0, 0, DateTimeKind.Utc), 1, 61.3271m, 500, 2 },
                    { 33, 3, new DateTime(2026, 3, 9, 19, 21, 0, 0, DateTimeKind.Utc), 2, 161.2136m, 60, 2 },
                    { 34, 4, new DateTime(2026, 3, 11, 19, 38, 0, 0, DateTimeKind.Utc), 1, 2170.1370m, 7, 2 },
                    { 35, 5, new DateTime(2026, 3, 13, 19, 55, 0, 0, DateTimeKind.Utc), 1, 131775.0000m, 1, 2 },
                    { 36, 1, new DateTime(2026, 3, 15, 20, 12, 0, 0, DateTimeKind.Utc), 2, 38.0819m, 200, 2 },
                    { 37, 2, new DateTime(2026, 3, 17, 20, 29, 0, 0, DateTimeKind.Utc), 1, 61.0813m, 300, 2 },
                    { 38, 3, new DateTime(2026, 3, 19, 20, 46, 0, 0, DateTimeKind.Utc), 1, 163.9735m, 40, 2 },
                    { 39, 4, new DateTime(2026, 3, 21, 21, 3, 0, 0, DateTimeKind.Utc), 2, 2161.5339m, 5, 2 },
                    { 40, 5, new DateTime(2026, 3, 23, 21, 20, 0, 0, DateTimeKind.Utc), 1, 131250.0000m, 6, 3 },
                    { 41, 1, new DateTime(2026, 3, 25, 21, 37, 0, 0, DateTimeKind.Utc), 1, 37.9294m, 700, 2 },
                    { 42, 2, new DateTime(2026, 3, 27, 21, 54, 0, 0, DateTimeKind.Utc), 2, 60.8355m, 100, 2 },
                    { 43, 3, new DateTime(2026, 3, 29, 22, 11, 0, 0, DateTimeKind.Utc), 1, 163.3241m, 20, 2 },
                    { 44, 4, new DateTime(2026, 3, 31, 22, 28, 0, 0, DateTimeKind.Utc), 1, 2152.9308m, 3, 2 },
                    { 45, 5, new DateTime(2026, 4, 2, 22, 45, 0, 0, DateTimeKind.Utc), 2, 130725.0000m, 4, 2 },
                    { 46, 1, new DateTime(2026, 4, 4, 23, 2, 0, 0, DateTimeKind.Utc), 1, 37.7769m, 500, 2 },
                    { 47, 2, new DateTime(2026, 4, 6, 23, 19, 0, 0, DateTimeKind.Utc), 1, 61.8802m, 600, 2 },
                    { 48, 3, new DateTime(2026, 4, 8, 23, 36, 0, 0, DateTimeKind.Utc), 2, 162.6747m, 70, 2 },
                    { 49, 4, new DateTime(2026, 4, 10, 23, 53, 0, 0, DateTimeKind.Utc), 1, 2144.3277m, 1, 2 },
                    { 50, 5, new DateTime(2026, 4, 13, 0, 10, 0, 0, DateTimeKind.Utc), 1, 130200.0000m, 2, 3 },
                    { 51, 1, new DateTime(2026, 4, 15, 0, 27, 0, 0, DateTimeKind.Utc), 2, 38.4250m, 300, 1 },
                    { 52, 2, new DateTime(2026, 4, 17, 0, 44, 0, 0, DateTimeKind.Utc), 1, 61.6344m, 400, 1 },
                    { 53, 3, new DateTime(2026, 4, 19, 1, 1, 0, 0, DateTimeKind.Utc), 1, 162.0253m, 50, 1 },
                    { 54, 4, new DateTime(2026, 4, 21, 1, 18, 0, 0, DateTimeKind.Utc), 2, 2135.7245m, 6, 1 },
                    { 55, 5, new DateTime(2026, 4, 23, 1, 35, 0, 0, DateTimeKind.Utc), 1, 132431.2500m, 7, 1 },
                    { 56, 1, new DateTime(2026, 4, 25, 1, 52, 0, 0, DateTimeKind.Utc), 1, 38.2725m, 100, 1 },
                    { 57, 2, new DateTime(2026, 4, 27, 2, 9, 0, 0, DateTimeKind.Utc), 2, 61.3886m, 200, 1 },
                    { 58, 3, new DateTime(2026, 4, 29, 2, 26, 0, 0, DateTimeKind.Utc), 1, 161.3759m, 30, 1 },
                    { 59, 4, new DateTime(2026, 5, 1, 2, 43, 0, 0, DateTimeKind.Utc), 1, 2172.2878m, 4, 1 },
                    { 60, 5, new DateTime(2026, 5, 3, 3, 0, 0, 0, DateTimeKind.Utc), 2, 131906.2500m, 5, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ativos_Ticker",
                table: "Ativos",
                column: "Ticker",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ordens_AtivoId",
                table: "Ordens",
                column: "AtivoId");

            migrationBuilder.CreateIndex(
                name: "IX_Ordens_DataExecucao_Status",
                table: "Ordens",
                columns: new[] { "DataExecucao", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ordens");

            migrationBuilder.DropTable(
                name: "Ativos");
        }
    }
}
