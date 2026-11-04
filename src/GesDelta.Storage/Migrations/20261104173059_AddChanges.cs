using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GesDelta.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "changes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    run_id = table.Column<long>(type: "bigint", nullable: false),
                    material_id = table.Column<long>(type: "bigint", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    prev_snapshot_id = table.Column<long>(type: "bigint", nullable: true),
                    new_snapshot_id = table.Column<long>(type: "bigint", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    error = table.Column<string>(type: "text", nullable: true),
                    done_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_changes", x => x.id);
                    table.ForeignKey(
                        name: "fk_changes_materials_material_id",
                        column: x => x.material_id,
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_changes_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_changes_snapshots_new_snapshot_id",
                        column: x => x.new_snapshot_id,
                        principalTable: "snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_changes_snapshots_prev_snapshot_id",
                        column: x => x.prev_snapshot_id,
                        principalTable: "snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_changes_created_at",
                table: "changes",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_changes_done_at",
                table: "changes",
                column: "done_at");

            migrationBuilder.CreateIndex(
                name: "ix_changes_material_id_id",
                table: "changes",
                columns: new[] { "material_id", "id" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_changes_new_snapshot_id",
                table: "changes",
                column: "new_snapshot_id");

            migrationBuilder.CreateIndex(
                name: "ix_changes_prev_snapshot_id",
                table: "changes",
                column: "prev_snapshot_id");

            migrationBuilder.CreateIndex(
                name: "ix_changes_run_id",
                table: "changes",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "ix_changes_status",
                table: "changes",
                column: "status",
                filter: "status = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "changes");
        }
    }
}
