using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaijaPrimeSchool.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssessmentsAndResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AffectiveTraits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
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
                    table.PrimaryKey("PK_AffectiveTraits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DefaultMaxScore = table.Column<int>(type: "int", nullable: false),
                    IsExam = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_AssessmentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GradeBands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LowerBound = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    UpperBound = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
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
                    table.PrimaryKey("PK_GradeBands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PsychomotorSkills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
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
                    table.PrimaryKey("PK_PsychomotorSkills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TermId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectsTaken = table.Column<int>(type: "int", nullable: false),
                    TotalScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    AveragePercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: true),
                    StudentsInClass = table.Column<int>(type: "int", nullable: true),
                    DaysPresent = table.Column<int>(type: "int", nullable: false),
                    DaysAbsent = table.Column<int>(type: "int", nullable: false),
                    DaysLate = table.Column<int>(type: "int", nullable: false),
                    TotalSchoolDays = table.Column<int>(type: "int", nullable: false),
                    ClassTeacherComment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HeadTeacherComment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NextTermBegins = table.Column<DateOnly>(type: "date", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    PublishedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_ReportCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportCards_SchoolClasses_SchoolClassId",
                        column: x => x.SchoolClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportCards_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportCards_Terms_TermId",
                        column: x => x.TermId,
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TraitRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Value = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TraitRatings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TermAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TermId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    MaxScore = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    AssessmentDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    PublishedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_TermAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TermAssessments_AssessmentTypes_AssessmentTypeId",
                        column: x => x.AssessmentTypeId,
                        principalTable: "AssessmentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TermAssessments_SchoolClasses_SchoolClassId",
                        column: x => x.SchoolClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TermAssessments_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TermAssessments_Terms_TermId",
                        column: x => x.TermId,
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubjectResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TermId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    GradeBandId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Position = table.Column<int>(type: "int", nullable: true),
                    StudentsInClass = table.Column<int>(type: "int", nullable: true),
                    TeacherComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsFinalised = table.Column<bool>(type: "bit", nullable: false),
                    FinalisedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_SubjectResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectResults_GradeBands_GradeBandId",
                        column: x => x.GradeBandId,
                        principalTable: "GradeBands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SubjectResults_SchoolClasses_SchoolClassId",
                        column: x => x.SchoolClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubjectResults_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubjectResults_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubjectResults_Terms_TermId",
                        column: x => x.TermId,
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AffectiveRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AffectiveTraitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TraitRatingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_AffectiveRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AffectiveRatings_AffectiveTraits_AffectiveTraitId",
                        column: x => x.AffectiveTraitId,
                        principalTable: "AffectiveTraits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AffectiveRatings_ReportCards_ReportCardId",
                        column: x => x.ReportCardId,
                        principalTable: "ReportCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AffectiveRatings_TraitRatings_TraitRatingId",
                        column: x => x.TraitRatingId,
                        principalTable: "TraitRatings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PsychomotorRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PsychomotorSkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TraitRatingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_PsychomotorRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PsychomotorRatings_PsychomotorSkills_PsychomotorSkillId",
                        column: x => x.PsychomotorSkillId,
                        principalTable: "PsychomotorSkills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PsychomotorRatings_ReportCards_ReportCardId",
                        column: x => x.ReportCardId,
                        principalTable: "ReportCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PsychomotorRatings_TraitRatings_TraitRatingId",
                        column: x => x.TraitRatingId,
                        principalTable: "TraitRatings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TermAssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    IsAbsent = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_AssessmentScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentScores_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentScores_TermAssessments_TermAssessmentId",
                        column: x => x.TermAssessmentId,
                        principalTable: "TermAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AffectiveRatings_AffectiveTraitId",
                table: "AffectiveRatings",
                column: "AffectiveTraitId");

            migrationBuilder.CreateIndex(
                name: "IX_AffectiveRatings_IsDeleted",
                table: "AffectiveRatings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AffectiveRatings_ReportCardId_AffectiveTraitId",
                table: "AffectiveRatings",
                columns: new[] { "ReportCardId", "AffectiveTraitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AffectiveRatings_TraitRatingId",
                table: "AffectiveRatings",
                column: "TraitRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_AffectiveTraits_Name",
                table: "AffectiveTraits",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentScores_IsDeleted",
                table: "AssessmentScores",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentScores_StudentId",
                table: "AssessmentScores",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentScores_TermAssessmentId_StudentId",
                table: "AssessmentScores",
                columns: new[] { "TermAssessmentId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentTypes_Code",
                table: "AssessmentTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentTypes_Name",
                table: "AssessmentTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeBands_Name",
                table: "GradeBands",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PsychomotorRatings_IsDeleted",
                table: "PsychomotorRatings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PsychomotorRatings_PsychomotorSkillId",
                table: "PsychomotorRatings",
                column: "PsychomotorSkillId");

            migrationBuilder.CreateIndex(
                name: "IX_PsychomotorRatings_ReportCardId_PsychomotorSkillId",
                table: "PsychomotorRatings",
                columns: new[] { "ReportCardId", "PsychomotorSkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PsychomotorRatings_TraitRatingId",
                table: "PsychomotorRatings",
                column: "TraitRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_PsychomotorSkills_Name",
                table: "PsychomotorSkills",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportCards_IsDeleted",
                table: "ReportCards",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ReportCards_SchoolClassId_TermId",
                table: "ReportCards",
                columns: new[] { "SchoolClassId", "TermId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportCards_StudentId_TermId",
                table: "ReportCards",
                columns: new[] { "StudentId", "TermId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportCards_TermId",
                table: "ReportCards",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectResults_GradeBandId",
                table: "SubjectResults",
                column: "GradeBandId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectResults_IsDeleted",
                table: "SubjectResults",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectResults_SchoolClassId_TermId_SubjectId",
                table: "SubjectResults",
                columns: new[] { "SchoolClassId", "TermId", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_SubjectResults_StudentId_TermId_SubjectId",
                table: "SubjectResults",
                columns: new[] { "StudentId", "TermId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubjectResults_SubjectId",
                table: "SubjectResults",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectResults_TermId",
                table: "SubjectResults",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_TermAssessments_AssessmentTypeId",
                table: "TermAssessments",
                column: "AssessmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TermAssessments_IsDeleted",
                table: "TermAssessments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TermAssessments_SchoolClassId",
                table: "TermAssessments",
                column: "SchoolClassId");

            migrationBuilder.CreateIndex(
                name: "IX_TermAssessments_SubjectId",
                table: "TermAssessments",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TermAssessments_TermId_SchoolClassId_SubjectId",
                table: "TermAssessments",
                columns: new[] { "TermId", "SchoolClassId", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_TraitRatings_Name",
                table: "TraitRatings",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TraitRatings_Value",
                table: "TraitRatings",
                column: "Value",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AffectiveRatings");

            migrationBuilder.DropTable(
                name: "AssessmentScores");

            migrationBuilder.DropTable(
                name: "PsychomotorRatings");

            migrationBuilder.DropTable(
                name: "SubjectResults");

            migrationBuilder.DropTable(
                name: "AffectiveTraits");

            migrationBuilder.DropTable(
                name: "TermAssessments");

            migrationBuilder.DropTable(
                name: "PsychomotorSkills");

            migrationBuilder.DropTable(
                name: "ReportCards");

            migrationBuilder.DropTable(
                name: "TraitRatings");

            migrationBuilder.DropTable(
                name: "GradeBands");

            migrationBuilder.DropTable(
                name: "AssessmentTypes");
        }
    }
}
