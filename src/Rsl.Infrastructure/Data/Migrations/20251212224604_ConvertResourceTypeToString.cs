using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rsl.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConvertResourceTypeToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop CurrentEvent-specific columns
            migrationBuilder.DropColumn(
                name: "BlogPost_Author",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "NewsOutlet",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Resources");

            // Add a temporary column to hold the string values
            migrationBuilder.AddColumn<string>(
                name: "Type_Temp",
                table: "Resources",
                type: "nvarchar(max)",
                nullable: true);

            // Convert existing integer values to string enum names
            migrationBuilder.Sql(@"
                UPDATE Resources
                SET Type_Temp = CASE Type
                    WHEN 0 THEN 'Paper'
                    WHEN 1 THEN 'Video'
                    WHEN 2 THEN 'BlogPost'
                    WHEN 3 THEN 'CurrentEvent'
                    WHEN 4 THEN 'SocialMediaPost'
                    ELSE 'Unknown'
                END
            ");

            // Drop the old Type column
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Resources");

            // Rename the temporary column to Type
            migrationBuilder.RenameColumn(
                name: "Type_Temp",
                table: "Resources",
                newName: "Type");

            // Make Type non-nullable
            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Resources",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add a temporary column to hold integer values
            migrationBuilder.AddColumn<int>(
                name: "Type_Temp",
                table: "Resources",
                type: "int",
                nullable: true);

            // Convert string values back to integers
            migrationBuilder.Sql(@"
                UPDATE Resources
                SET Type_Temp = CASE Type
                    WHEN 'Paper' THEN 0
                    WHEN 'Video' THEN 1
                    WHEN 'BlogPost' THEN 2
                    WHEN 'CurrentEvent' THEN 3
                    WHEN 'SocialMediaPost' THEN 4
                    ELSE 0
                END
            ");

            // Drop the string Type column
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Resources");

            // Rename the temporary column to Type
            migrationBuilder.RenameColumn(
                name: "Type_Temp",
                table: "Resources",
                newName: "Type");

            // Make Type non-nullable
            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Resources",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // Re-add CurrentEvent-specific columns
            migrationBuilder.AddColumn<string>(
                name: "BlogPost_Author",
                table: "Resources",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewsOutlet",
                table: "Resources",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Resources",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
