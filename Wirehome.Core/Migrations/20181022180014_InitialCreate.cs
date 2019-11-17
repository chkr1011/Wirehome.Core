using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Wirehome.Core.Migrations
{
    public partial class InitialCreate //: Migration
    {
        //protected override void Up(MigrationBuilder migrationBuilder)
        //{
        //    migrationBuilder.CreateTable(
        //        name: "ComponentStatus",
        //        columns: table => new
        //        {
        //            ID = table.Column<ulong>(nullable: false)
        //                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
        //            ComponentUid = table.Column<string>(maxLength: 256, nullable: true),
        //            StatusUid = table.Column<string>(maxLength: 256, nullable: true),
        //            Value = table.Column<string>(maxLength: 512, nullable: true),
        //            RangeStart = table.Column<DateTime>(nullable: false),
        //            RangeEnd = table.Column<DateTime>(nullable: false),
        //            PreviousEntityID = table.Column<ulong>(nullable: true),
        //            NextEntityID = table.Column<ulong>(nullable: true)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_ComponentStatus", x => x.ID);
        //            table.ForeignKey(
        //                name: "FK_ComponentStatus_ComponentStatus_NextEntityID",
        //                column: x => x.NextEntityID,
        //                principalTable: "ComponentStatus",
        //                principalColumn: "ID",
        //                onDelete: ReferentialAction.Restrict);
        //            table.ForeignKey(
        //                name: "FK_ComponentStatus_ComponentStatus_PreviousEntityID",
        //                column: x => x.PreviousEntityID,
        //                principalTable: "ComponentStatus",
        //                principalColumn: "ID",
        //                onDelete: ReferentialAction.Restrict);
        //        });

        //    migrationBuilder.CreateIndex(
        //        name: "IX_ComponentStatus_NextEntityID",
        //        table: "ComponentStatus",
        //        column: "NextEntityID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "IX_ComponentStatus_PreviousEntityID",
        //        table: "ComponentStatus",
        //        column: "PreviousEntityID");

        //    migrationBuilder.CreateIndex(
        //        name: "IX_ComponentStatus_RangeStart_RangeEnd_ComponentUid_StatusUid",
        //        table: "ComponentStatus",
        //        columns: new[] { "RangeStart", "RangeEnd", "ComponentUid", "StatusUid" });
        //}

        //protected override void Down(MigrationBuilder migrationBuilder)
        //{
        //    migrationBuilder.DropTable(
        //        name: "ComponentStatus");
        //}
    }
}
