using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GesDelta.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddGinIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE INDEX ix_claims_text_fts ON claims USING GIN (to_tsvector('simple', text));");
            migrationBuilder.Sql(
                "CREATE INDEX ix_snapshots_clean_text_fts ON snapshots USING GIN (to_tsvector('simple', clean_text));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX ix_claims_text_fts;");
            migrationBuilder.Sql("DROP INDEX ix_snapshots_clean_text_fts;");
        }
    }
}
