using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameResourceToContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recommendations_Resources_ResourceId",
                table: "Recommendations");

            migrationBuilder.DropForeignKey(
                name: "FK_ResourceVotes_Resources_ResourceId",
                table: "ResourceVotes");

            migrationBuilder.DropForeignKey(
                name: "FK_ResourceVotes_Users_UserId",
                table: "ResourceVotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Resources_Sources_SourceId",
                table: "Resources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Resources",
                table: "Resources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ResourceVotes",
                table: "ResourceVotes");

            migrationBuilder.RenameTable(
                name: "Resources",
                newName: "Content");

            migrationBuilder.RenameTable(
                name: "ResourceVotes",
                newName: "ContentVotes");

            migrationBuilder.RenameColumn(
                name: "ResourceId",
                table: "Recommendations",
                newName: "ContentId");

            migrationBuilder.RenameColumn(
                name: "ResourceId",
                table: "ContentVotes",
                newName: "ContentId");

            migrationBuilder.RenameIndex(
                name: "IX_Recommendations_ResourceId",
                table: "Recommendations",
                newName: "IX_Recommendations_ContentId");

            migrationBuilder.RenameIndex(
                name: "IX_Resources_SourceId",
                table: "Content",
                newName: "IX_Content_SourceId");

            migrationBuilder.RenameIndex(
                name: "IX_Resources_Url",
                table: "Content",
                newName: "IX_Content_Url");

            migrationBuilder.RenameIndex(
                name: "IX_ResourceVotes_ResourceId",
                table: "ContentVotes",
                newName: "IX_ContentVotes_ContentId");

            migrationBuilder.RenameIndex(
                name: "IX_ResourceVotes_UserId_ResourceId",
                table: "ContentVotes",
                newName: "IX_ContentVotes_UserId_ContentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Content",
                table: "Content",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ContentVotes",
                table: "ContentVotes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Content_Sources_SourceId",
                table: "Content",
                column: "SourceId",
                principalTable: "Sources",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ContentVotes_Content_ContentId",
                table: "ContentVotes",
                column: "ContentId",
                principalTable: "Content",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ContentVotes_Users_UserId",
                table: "ContentVotes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Recommendations_Content_ContentId",
                table: "Recommendations",
                column: "ContentId",
                principalTable: "Content",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Content_Sources_SourceId",
                table: "Content");

            migrationBuilder.DropForeignKey(
                name: "FK_ContentVotes_Content_ContentId",
                table: "ContentVotes");

            migrationBuilder.DropForeignKey(
                name: "FK_ContentVotes_Users_UserId",
                table: "ContentVotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Recommendations_Content_ContentId",
                table: "Recommendations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Content",
                table: "Content");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ContentVotes",
                table: "ContentVotes");

            migrationBuilder.RenameTable(
                name: "Content",
                newName: "Resources");

            migrationBuilder.RenameTable(
                name: "ContentVotes",
                newName: "ResourceVotes");

            migrationBuilder.RenameColumn(
                name: "ContentId",
                table: "Recommendations",
                newName: "ResourceId");

            migrationBuilder.RenameColumn(
                name: "ContentId",
                table: "ResourceVotes",
                newName: "ResourceId");

            migrationBuilder.RenameIndex(
                name: "IX_Recommendations_ContentId",
                table: "Recommendations",
                newName: "IX_Recommendations_ResourceId");

            migrationBuilder.RenameIndex(
                name: "IX_Content_SourceId",
                table: "Resources",
                newName: "IX_Resources_SourceId");

            migrationBuilder.RenameIndex(
                name: "IX_Content_Url",
                table: "Resources",
                newName: "IX_Resources_Url");

            migrationBuilder.RenameIndex(
                name: "IX_ContentVotes_ContentId",
                table: "ResourceVotes",
                newName: "IX_ResourceVotes_ResourceId");

            migrationBuilder.RenameIndex(
                name: "IX_ContentVotes_UserId_ContentId",
                table: "ResourceVotes",
                newName: "IX_ResourceVotes_UserId_ResourceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Resources",
                table: "Resources",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ResourceVotes",
                table: "ResourceVotes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Recommendations_Resources_ResourceId",
                table: "Recommendations",
                column: "ResourceId",
                principalTable: "Resources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceVotes_Resources_ResourceId",
                table: "ResourceVotes",
                column: "ResourceId",
                principalTable: "Resources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceVotes_Users_UserId",
                table: "ResourceVotes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_Sources_SourceId",
                table: "Resources",
                column: "SourceId",
                principalTable: "Sources",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
