using System.ComponentModel.DataAnnotations;

namespace NaijaPrimeSchool.Application.Results.Dtos;

public class TermAssessmentDto
{
    public Guid Id { get; set; }

    public Guid TermId { get; set; }
    public string TermName { get; set; } = string.Empty;

    public Guid SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;

    public Guid SchoolClassId { get; set; }
    public string SchoolClassName { get; set; } = string.Empty;

    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;

    public Guid AssessmentTypeId { get; set; }
    public string AssessmentTypeName { get; set; } = string.Empty;
    public string AssessmentTypeCode { get; set; } = string.Empty;
    public bool IsExam { get; set; }

    public string Title { get; set; } = string.Empty;
    public int MaxScore { get; set; }
    public decimal Weight { get; set; }
    public DateOnly? AssessmentDate { get; set; }

    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedOn { get; set; }

    public string? Notes { get; set; }

    public int ScoredCount { get; set; }
    public int ExpectedCount { get; set; }
}

public class CreateTermAssessmentRequest
{
    [Required] public Guid TermId { get; set; }
    [Required] public Guid SchoolClassId { get; set; }
    [Required] public Guid SubjectId { get; set; }
    [Required] public Guid AssessmentTypeId { get; set; }

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Range(1, 1000)]
    public int MaxScore { get; set; } = 100;

    [Range(0.0, 100.0)]
    public decimal Weight { get; set; } = 1m;

    public DateOnly? AssessmentDate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class UpdateTermAssessmentRequest
{
    public Guid Id { get; set; }
    [Required] public Guid AssessmentTypeId { get; set; }

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Range(1, 1000)]
    public int MaxScore { get; set; }

    [Range(0.0, 100.0)]
    public decimal Weight { get; set; }

    public DateOnly? AssessmentDate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class TermAssessmentFilter
{
    public Guid? TermId { get; set; }
    public Guid? SchoolClassId { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? AssessmentTypeId { get; set; }
    public bool? IsPublished { get; set; }
}
