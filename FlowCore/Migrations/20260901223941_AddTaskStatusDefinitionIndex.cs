using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowCore.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskStatusDefinitionIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_TaskStatusDefinitionId",
                table: "TaskItems",
                column: "TaskStatusDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskItems_TaskStatusDefinitionId",
                table: "TaskItems");
        }
    }
}
