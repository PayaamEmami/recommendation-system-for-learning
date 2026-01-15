using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rsl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddXIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "XConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    XUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Handle = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AccessTokenEncrypted = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    RefreshTokenEncrypted = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    TokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Scopes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ConnectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XConnections_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "XFollowedAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    XUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Handle = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProfileImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FollowedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XFollowedAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XFollowedAccounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "XSelectedAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    XFollowedAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XSelectedAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XSelectedAccounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_XSelectedAccounts_XFollowedAccounts_XFollowedAccountId",
                        column: x => x.XFollowedAccountId,
                        principalTable: "XFollowedAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "XPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    XSelectedAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PostCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AuthorXUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AuthorHandle = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AuthorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AuthorProfileImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MediaJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    LikeCount = table.Column<int>(type: "integer", nullable: false),
                    ReplyCount = table.Column<int>(type: "integer", nullable: false),
                    RepostCount = table.Column<int>(type: "integer", nullable: false),
                    QuoteCount = table.Column<int>(type: "integer", nullable: false),
                    IngestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XPosts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_XPosts_XSelectedAccounts_XSelectedAccountId",
                        column: x => x.XSelectedAccountId,
                        principalTable: "XSelectedAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_XConnections_UserId",
                table: "XConnections",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_XConnections_XUserId",
                table: "XConnections",
                column: "XUserId");

            migrationBuilder.CreateIndex(
                name: "IX_XFollowedAccounts_UserId_XUserId",
                table: "XFollowedAccounts",
                columns: new[] { "UserId", "XUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_XSelectedAccounts_UserId_XFollowedAccountId",
                table: "XSelectedAccounts",
                columns: new[] { "UserId", "XFollowedAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_XSelectedAccounts_XFollowedAccountId",
                table: "XSelectedAccounts",
                column: "XFollowedAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_XPosts_UserId_PostCreatedAt",
                table: "XPosts",
                columns: new[] { "UserId", "PostCreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_XPosts_UserId_PostId",
                table: "XPosts",
                columns: new[] { "UserId", "PostId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_XPosts_XSelectedAccountId",
                table: "XPosts",
                column: "XSelectedAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "XConnections");

            migrationBuilder.DropTable(
                name: "XPosts");

            migrationBuilder.DropTable(
                name: "XSelectedAccounts");

            migrationBuilder.DropTable(
                name: "XFollowedAccounts");
        }
    }
}
