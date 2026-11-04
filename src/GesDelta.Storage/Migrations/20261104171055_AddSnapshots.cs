using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GesDelta.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "snapshots",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    material_id = table.Column<long>(type: "bigint", nullable: false),
                    run_id = table.Column<long>(type: "bigint", nullable: false),
                    fetched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    raw_text = table.Column<string>(type: "text", nullable: false),
                    clean_text = table.Column<string>(type: "text", nullable: false),
                    content_hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "fk_snapshots_materials_material_id",
                        column: x => x.material_id,
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_snapshots_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_snapshots_material_id_fetched_at",
                table: "snapshots",
                columns: new[] { "material_id", "fetched_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_snapshots_run_id",
                table: "snapshots",
                column: "run_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "snapshots");
        }
    }
}
