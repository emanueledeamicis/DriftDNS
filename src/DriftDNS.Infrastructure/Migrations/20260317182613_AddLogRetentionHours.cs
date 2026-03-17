using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriftDNS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLogRetentionHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LogRetentionHours",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 24);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogRetentionHours",
                table: "AppSettings");
        }
    }
}
