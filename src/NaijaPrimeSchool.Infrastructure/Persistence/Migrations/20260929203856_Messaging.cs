using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaijaPrimeSchool.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Messaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MessageThreadStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageThreadStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageThreads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FamilyUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MessageThreadStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedByStaff = table.Column<bool>(type: "bit", nullable: false),
                    LastMessageOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastMessageFromStaff = table.Column<bool>(type: "bit", nullable: false),
                    FamilyLastReadOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    StaffLastReadOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageThreads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageThreads_MessageThreadStatuses_MessageThreadStatusId",
                        column: x => x.MessageThreadStatusId,
                        principalTable: "MessageThreadStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MessageThreads_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MessageThreads_Users_FamilyUserId",
                        column: x => x.FamilyUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThreadMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageThreadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsFromStaff = table.Column<bool>(type: "bit", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SentOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreadMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThreadMessages_MessageThreads_MessageThreadId",
                        column: x => x.MessageThreadId,
                        principalTable: "MessageThreads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ThreadMessages_Users_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MessageThreads_FamilyUserId_LastMessageOn",
                table: "MessageThreads",
                columns: new[] { "FamilyUserId", "LastMessageOn" });

            migrationBuilder.CreateIndex(
                name: "IX_MessageThreads_IsDeleted",
                table: "MessageThreads",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MessageThreads_LastMessageOn",
                table: "MessageThreads",
                column: "LastMessageOn");

            migrationBuilder.CreateIndex(
                name: "IX_MessageThreads_MessageThreadStatusId",
                table: "MessageThreads",
                column: "MessageThreadStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageThreads_StudentId",
                table: "MessageThreads",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageThreadStatuses_Code",
                table: "MessageThreadStatuses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageThreadStatuses_Name",
                table: "MessageThreadStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThreadMessages_IsDeleted",
                table: "ThreadMessages",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ThreadMessages_MessageThreadId_SentOn",
                table: "ThreadMessages",
                columns: new[] { "MessageThreadId", "SentOn" });

            migrationBuilder.CreateIndex(
                name: "IX_ThreadMessages_SenderUserId",
                table: "ThreadMessages",
                column: "SenderUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ThreadMessages");

            migrationBuilder.DropTable(
                name: "MessageThreads");

            migrationBuilder.DropTable(
                name: "MessageThreadStatuses");
        }
    }
}
