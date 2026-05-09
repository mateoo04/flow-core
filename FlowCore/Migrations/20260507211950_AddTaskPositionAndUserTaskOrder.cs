using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowCore.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskPositionAndUserTaskOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskItems_TaskStatusDefinitionId",
                table: "TaskItems");

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "TaskItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
    UPDATE ""TaskItems"" t
    SET ""Position"" = sub.rn - 1
    FROM (
        SELECT ""Id"", ROW_NUMBER() OVER (PARTITION BY ""TaskStatusDefinitionId"" ORDER BY ""Title"") AS rn
        FROM ""TaskItems""
    ) sub
    WHERE t.""Id"" = sub.""Id"";
");

            migrationBuilder.CreateTable(
                name: "UserTaskOrders",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTaskOrders", x => new { x.UserId, x.TaskItemId });
                    table.ForeignKey(
                        name: "FK_UserTaskOrders_TaskItems_TaskItemId",
                        column: x => x.TaskItemId,
                        principalTable: "TaskItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserTaskOrders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_TaskStatusDefinitionId_Position",
                table: "TaskItems",
                columns: new[] { "TaskStatusDefinitionId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_UserTaskOrders_TaskItemId",
                table: "UserTaskOrders",
                column: "TaskItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserTaskOrders");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_TaskStatusDefinitionId_Position",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "TaskItems");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_TaskStatusDefinitionId",
                table: "TaskItems",
                column: "TaskStatusDefinitionId");
        }
    }
}
