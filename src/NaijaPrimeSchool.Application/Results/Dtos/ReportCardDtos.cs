using System.ComponentModel.DataAnnotations;

namespace NaijaPrimeSchool.Application.Results.Dtos;

public class ReportCardDto
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentAdmissionNumber { get; set; } = string.Empty;
    public string? StudentPhotoUrl { get; set; }
    public string StudentFirstName { get; set; } = string.Empty;
    public string StudentLastName { get; set; } = string.Empty;

    public Guid TermId { get; set; }
    public string TermName { get; set; } = string.Empty;

    public Guid SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;

    public Guid SchoolClassId { get; set; }
    public string SchoolClassName { get; set; } = string.Empty;

    public int SubjectsTaken { get; set; }
    public decimal TotalScore { get; set; }
    public decimal AveragePercentage { get; set; }

    public int? Position { get; set; }
    public int? StudentsInClass { get; set; }

    public int DaysPresent { get; set; }
    public int DaysAbsent { get; set; }
    public int DaysLate { get; set; }
    public int TotalSchoolDays { get; set; }

    public string? ClassTeacherComment { get; set; }
    public string? HeadTeacherComment { get; set; }

    public DateOnly? NextTermBegins { get; set; }

    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedOn { get; set; }
}

public class AffectiveRatingDto
{
    public Guid Id { get; set; }
    public Guid AffectiveTraitId { get; set; }
    public string AffectiveTraitName { get; set; } = string.Empty;
    public Guid TraitRatingId { get; set; }
    public string TraitRatingName { get; set; } = string.Empty;
    public int TraitRatingValue { get; set; }
}

public class PsychomotorRatingDto
{
    public Guid Id { get; set; }
    public Guid PsychomotorSkillId { get; set; }
    public string PsychomotorSkillName { get; set; } = string.Empty;
    public Guid TraitRatingId { get; set; }
    public string TraitRatingName { get; set; } = string.Empty;
    public int TraitRatingValue { get; set; }
}

public class ReportCardDetailDto
{
    public ReportCardDto Card { get; set; } = new();
    public List<SubjectResultDto> Results { get; set; } = [];
    public List<AffectiveRatingDto> AffectiveRatings { get; set; } = [];
    public List<PsychomotorRatingDto> PsychomotorRatings { get; set; } = [];
}

public class GenerateReportCardsRequest
{
    [Required] public Guid TermId { get; set; }
    [Required] public Guid SchoolClassId { get; set; }
    public DateOnly? NextTermBegins { get; set; }
}

public class GenerateReportCardsResponse
{
    public int CardsGenerated { get; set; }
    public int CardsUpdated { get; set; }
    public List<string> Warnings { get; set; } = [];
}

public class UpdateReportCardCommentsRequest
{
    public Guid Id { get; set; }

    [StringLength(1000)]
    public string? ClassTeacherComment { get; set; }

    [StringLength(1000)]
    public string? HeadTeacherComment { get; set; }

    public DateOnly? NextTermBegins { get; set; }
}

public class UpsertAffectiveRatingRequest
{
    [Required] public Guid ReportCardId { get; set; }
    [Required] public Guid AffectiveTraitId { get; set; }
    [Required] public Guid TraitRatingId { get; set; }
}

public class UpsertPsychomotorRatingRequest
{
    [Required] public Guid ReportCardId { get; set; }
    [Required] public Guid PsychomotorSkillId { get; set; }
    [Required] public Guid TraitRatingId { get; set; }
}

public class ReportCardFilter
{
    public Guid? TermId { get; set; }
    public Guid? SchoolClassId { get; set; }
    public Guid? StudentId { get; set; }
    public bool? IsPublished { get; set; }
}
