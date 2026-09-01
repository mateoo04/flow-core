using System;
using FlowCore.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowCore.Migrations;

[DbContext(typeof(FlowCoreDbContext))]
[Migration("20260902120000_RemoveWorkspaceVisibilityAndArchive")]
public partial class RemoveWorkspaceVisibilityAndArchive : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ArchivedAt",
            table: "Workspaces");

        migrationBuilder.DropColumn(
            name: "Visibility",
            table: "Workspaces");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "ArchivedAt",
            table: "Workspaces",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "Visibility",
            table: "Workspaces",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }
}
