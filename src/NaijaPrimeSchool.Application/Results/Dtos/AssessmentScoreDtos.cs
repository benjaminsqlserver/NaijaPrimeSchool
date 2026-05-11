using System.ComponentModel.DataAnnotations;

namespace NaijaPrimeSchool.Application.Results.Dtos;

public class AssessmentScoreDto
{
    public Guid Id { get; set; }
    public Guid TermAssessmentId { get; set; }

    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentAdmissionNumber { get; set; } = string.Empty;
    public string? StudentPhotoUrl { get; set; }
    public string StudentFirstName { get; set; } = string.Empty;
    public string StudentLastName { get; set; } = string.Empty;

    public decimal? Score { get; set; }
    public bool IsAbsent { get; set; }
    public string? Remarks { get; set; }
}

public class AssessmentScoreSheetDto
{
    public TermAssessmentDto Assessment { get; set; } = new();
    public List<AssessmentScoreDto> Scores { get; set; } = [];
}

public class UpsertAssessmentScoreRequest
{
    public Guid? Id { get; set; }
    [Required] public Guid TermAssessmentId { get; set; }
    [Required] public Guid StudentId { get; set; }
    public decimal? Score { get; set; }
    public bool IsAbsent { get; set; }

    [StringLength(300)]
    public string? Remarks { get; set; }
}

public class BulkSetScoresRequest
{
    [Required] public Guid TermAssessmentId { get; set; }
    public List<UpsertAssessmentScoreRequest> Scores { get; set; } = [];
}
