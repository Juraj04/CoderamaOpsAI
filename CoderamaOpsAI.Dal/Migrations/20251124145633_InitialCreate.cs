using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CoderamaOpsAI.Dal.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            // Seed test users with BCrypt hashed passwords
            // User 1: admin@example.com / Admin123!
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Name", "Email", "Password" },
                values: new object[] { "Admin User", "admin@example.com", "$2a$11$h1SshOnG9roANXexVRnuNeMrgKYNZ1f7Gl26Nm8kVtYrBOfyxdola" });

            // User 2: test@example.com / Test123!
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Name", "Email", "Password" },
                values: new object[] { "Test User", "test@example.com", "$2a$11$B0pKbxsyzuQ0Mv7dJO1M2u1qJW8L8Mw3JQqJYv8NgT.j4D1GNsMTi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
