using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GesDelta.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "claim_evidence",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    claim_id = table.Column<long>(type: "bigint", nullable: false),
                    snapshot_id = table.Column<long>(type: "bigint", nullable: false),
                    quote_original = table.Column<string>(type: "text", nullable: false),
                    quote_ru = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_claim_evidence", x => x.id);
                    table.ForeignKey(
                        name: "fk_claim_evidence_claims_claim_id",
                        column: x => x.claim_id,
                        principalTable: "claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_claim_evidence_snapshots_snapshot_id",
                        column: x => x.snapshot_id,
                        principalTable: "snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_claim_evidence_claim_id",
                table: "claim_evidence",
                column: "claim_id");

            migrationBuilder.CreateIndex(
                name: "ix_claim_evidence_snapshot_id",
                table: "claim_evidence",
                column: "snapshot_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "claim_evidence");
        }
    }
}
