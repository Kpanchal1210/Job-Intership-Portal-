using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Job_InternshipPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddJobLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxApplicants",
                table: "Jobs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PositionsAvailable",
                table: "Jobs",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxApplicants",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "PositionsAvailable",
                table: "Jobs");
        }
    }
}
