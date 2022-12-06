using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAPI_JWT_Auth.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TbUser",
                columns: table => new
                {
                    UserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    PasswordSalt = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    LoginAttempt = table.Column<int>(type: "int", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TokenCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TokenExpires = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerificationToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PasswordResetToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResetTokenExpires = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StateID = table.Column<int>(type: "int", nullable: false),
                    UserProfileID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TbUser", x => x.UserID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TbUser");
        }
    }
}
