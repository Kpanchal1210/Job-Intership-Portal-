using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Job_InternshipPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedDateToJobApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SelectedDate",
                table: "JobApplications",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedDate",
                table: "JobApplications");
        }
    }
}
