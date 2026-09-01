using FlowCore.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowCore.Migrations;

[DbContext(typeof(FlowCoreDbContext))]
[Migration("20260902121500_ScopeTaskPositionsToBoard")]
public partial class ScopeTaskPositionsToBoard : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_TaskItems_TaskStatusDefinitionId_Position",
            table: "TaskItems");

        migrationBuilder.CreateIndex(
            name: "IX_TaskItems_BoardId_TaskStatusDefinitionId_Position",
            table: "TaskItems",
            columns: new[] { "BoardId", "TaskStatusDefinitionId", "Position" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_TaskItems_BoardId_TaskStatusDefinitionId_Position",
            table: "TaskItems");

        migrationBuilder.CreateIndex(
            name: "IX_TaskItems_TaskStatusDefinitionId_Position",
            table: "TaskItems",
            columns: new[] { "TaskStatusDefinitionId", "Position" });
    }
}
