using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddXAuthState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "XAuthStates",
                columns: table => new
                {
                    State = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeVerifier = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RedirectUri = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XAuthStates", x => x.State);
                });

            migrationBuilder.CreateIndex(
                name: "IX_XAuthStates_ExpiresAt",
                table: "XAuthStates",
                column: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "XAuthStates");
        }
    }
}
