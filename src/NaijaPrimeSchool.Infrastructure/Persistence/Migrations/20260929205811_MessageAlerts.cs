using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaijaPrimeSchool.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MessageAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AnnouncementNotifications_AnnouncementId_UserId_NotificationChannelId",
                table: "AnnouncementNotifications");

            migrationBuilder.AlterColumn<Guid>(
                name: "AnnouncementId",
                table: "AnnouncementNotifications",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "MessageThreadId",
                table: "AnnouncementNotifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementNotifications_AnnouncementId_UserId_NotificationChannelId",
                table: "AnnouncementNotifications",
                columns: new[] { "AnnouncementId", "UserId", "NotificationChannelId" },
                unique: true,
                filter: "[AnnouncementId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementNotifications_MessageThreadId_UserId_NotificationChannelId",
                table: "AnnouncementNotifications",
                columns: new[] { "MessageThreadId", "UserId", "NotificationChannelId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_AnnouncementNotifications_OneSource",
                table: "AnnouncementNotifications",
                sql: "(CASE WHEN [AnnouncementId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [MessageThreadId] IS NULL THEN 0 ELSE 1 END) = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_AnnouncementNotifications_MessageThreads_MessageThreadId",
                table: "AnnouncementNotifications",
                column: "MessageThreadId",
                principalTable: "MessageThreads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Message alerts cannot exist once AnnouncementId is required again.
            migrationBuilder.Sql("DELETE FROM AnnouncementNotifications WHERE MessageThreadId IS NOT NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_AnnouncementNotifications_MessageThreads_MessageThreadId",
                table: "AnnouncementNotifications");

            migrationBuilder.DropIndex(
                name: "IX_AnnouncementNotifications_AnnouncementId_UserId_NotificationChannelId",
                table: "AnnouncementNotifications");

            migrationBuilder.DropIndex(
                name: "IX_AnnouncementNotifications_MessageThreadId_UserId_NotificationChannelId",
                table: "AnnouncementNotifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AnnouncementNotifications_OneSource",
                table: "AnnouncementNotifications");

            migrationBuilder.DropColumn(
                name: "MessageThreadId",
                table: "AnnouncementNotifications");

            migrationBuilder.AlterColumn<Guid>(
                name: "AnnouncementId",
                table: "AnnouncementNotifications",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementNotifications_AnnouncementId_UserId_NotificationChannelId",
                table: "AnnouncementNotifications",
                columns: new[] { "AnnouncementId", "UserId", "NotificationChannelId" },
                unique: true);
        }
    }
}
