using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowCore.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTaskAssignmentRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "TaskAssignments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "TaskAssignments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
