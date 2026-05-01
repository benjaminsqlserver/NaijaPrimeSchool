using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaijaPrimeSchool.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Attendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CountsAsPresent = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_AttendanceStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyAttendanceRegisters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TermId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TakenById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TakenOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    SubmittedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_DailyAttendanceRegisters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyAttendanceRegisters_SchoolClasses_SchoolClassId",
                        column: x => x.SchoolClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyAttendanceRegisters_Terms_TermId",
                        column: x => x.TermId,
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyAttendanceRegisters_Users_TakenById",
                        column: x => x.TakenById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SubjectAttendanceSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimetableEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TakenById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TakenOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    SubmittedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_SubjectAttendanceSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectAttendanceSessions_TimetableEntries_TimetableEntryId",
                        column: x => x.TimetableEntryId,
                        principalTable: "TimetableEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubjectAttendanceSessions_Users_TakenById",
                        column: x => x.TakenById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DailyAttendanceEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegisterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendanceStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArrivalTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("PK_DailyAttendanceEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyAttendanceEntries_AttendanceStatuses_AttendanceStatusId",
                        column: x => x.AttendanceStatusId,
                        principalTable: "AttendanceStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyAttendanceEntries_DailyAttendanceRegisters_RegisterId",
                        column: x => x.RegisterId,
                        principalTable: "DailyAttendanceRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DailyAttendanceEntries_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubjectAttendanceEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendanceStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("PK_SubjectAttendanceEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectAttendanceEntries_AttendanceStatuses_AttendanceStatusId",
                        column: x => x.AttendanceStatusId,
                        principalTable: "AttendanceStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubjectAttendanceEntries_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubjectAttendanceEntries_SubjectAttendanceSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "SubjectAttendanceSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceStatuses_Code",
                table: "AttendanceStatuses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceStatuses_Name",
                table: "AttendanceStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyAttendanceEntries_AttendanceStatusId",
                table: "DailyAttendanceEntries",
                column: "AttendanceStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyAttendanceEntries_IsDeleted",
                table: "DailyAttendanceEntries",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DailyAttendanceEntries_RegisterId_StudentId",
                table: "DailyAttendanceEntries",
                columns: new[] { "RegisterId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyAttendanceEntries_StudentId",
                table: "DailyAttendanceEntries",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyAttendanceRegisters_Date",
                table: "DailyAttendanceRegisters",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_DailyAttendanceRegisters_IsDeleted",
                table: "DailyAttendanceRegisters",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DailyAttendanceRegisters_SchoolClassId_Date",
                table: "DailyAttendanceRegisters",
                columns: new[] { "SchoolClassId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyAttendanceRegisters_TakenById",
                table: "DailyAttendanceRegisters",
                column: "TakenById");

            migrationBuilder.CreateIndex(
                name: "IX_DailyAttendanceRegisters_TermId",
                table: "DailyAttendanceRegisters",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectAttendanceEntries_AttendanceStatusId",
                table: "SubjectAttendanceEntries",
                column: "AttendanceStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectAttendanceEntries_IsDeleted",
                table: "SubjectAttendanceEntries",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectAttendanceEntries_SessionId_StudentId",
                table: "SubjectAttendanceEntries",
                columns: new[] { "SessionId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubjectAttendanceEntries_StudentId",
                table: "SubjectAttendanceEntries",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectAttendanceSessions_Date",
                table: "SubjectAttendanceSessions",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectAttendanceSessions_IsDeleted",
                table: "SubjectAttendanceSessions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectAttendanceSessions_TakenById",
                table: "SubjectAttendanceSessions",
                column: "TakenById");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectAttendanceSessions_TimetableEntryId_Date",
                table: "SubjectAttendanceSessions",
                columns: new[] { "TimetableEntryId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyAttendanceEntries");

            migrationBuilder.DropTable(
                name: "SubjectAttendanceEntries");

            migrationBuilder.DropTable(
                name: "DailyAttendanceRegisters");

            migrationBuilder.DropTable(
                name: "AttendanceStatuses");

            migrationBuilder.DropTable(
                name: "SubjectAttendanceSessions");
        }
    }
}
