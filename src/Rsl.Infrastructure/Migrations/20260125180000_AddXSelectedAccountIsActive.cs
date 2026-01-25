using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rsl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddXSelectedAccountIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "XSelectedAccounts",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "XSelectedAccounts");
        }
    }
}
