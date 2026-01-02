using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FidelityCard.Srv.Data.Migrations
{
    /// <inheritdoc />
    public partial class FidelityStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CdNe",
                table: "Fidelity",
                type: "varchar(6)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CdNe",
                table: "Fidelity");
        }
    }
}
