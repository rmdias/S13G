using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocuments_IssuerCnpj",
                table: "FiscalDocuments",
                column: "IssuerCnpj");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocuments_RecipientCnpj",
                table: "FiscalDocuments",
                column: "RecipientCnpj");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocuments_State",
                table: "FiscalDocuments",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocuments_IssueDate",
                table: "FiscalDocuments",
                column: "IssueDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FiscalDocuments_IssuerCnpj",
                table: "FiscalDocuments");

            migrationBuilder.DropIndex(
                name: "IX_FiscalDocuments_RecipientCnpj",
                table: "FiscalDocuments");

            migrationBuilder.DropIndex(
                name: "IX_FiscalDocuments_State",
                table: "FiscalDocuments");

            migrationBuilder.DropIndex(
                name: "IX_FiscalDocuments_IssueDate",
                table: "FiscalDocuments");
        }
    }
}
