using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PremierLottoApi.Migrations
{
    /// <inheritdoc />
    public partial class AddJackpotRollover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JackpotRollovers",
                columns: table => new
                {
                    GameType = table.Column<string>(type: "text", nullable: false),
                    CarriedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JackpotRollovers", x => x.GameType);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JackpotRollovers");
        }
    }
}
