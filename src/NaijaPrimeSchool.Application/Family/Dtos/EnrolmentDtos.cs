using System.ComponentModel.DataAnnotations;

namespace NaijaPrimeSchool.Application.Family.Dtos;

public class EnrolmentDto
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentAdmissionNumber { get; set; } = string.Empty;

    public Guid SchoolClassId { get; set; }
    public string SchoolClassName { get; set; } = string.Empty;

    public Guid SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;

    public DateOnly EnrolledOn { get; set; }
    public DateOnly? WithdrawnOn { get; set; }

    public Guid EnrolmentStatusId { get; set; }
    public string EnrolmentStatusName { get; set; } = string.Empty;

    public string? Notes { get; set; }
}

public class CreateEnrolmentRequest
{
    [Required] public Guid StudentId { get; set; }
    [Required] public Guid SchoolClassId { get; set; }
    [Required] public DateOnly EnrolledOn { get; set; }
    public Guid? EnrolmentStatusId { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class UpdateEnrolmentRequest
{
    public Guid Id { get; set; }
    [Required] public Guid SchoolClassId { get; set; }
    [Required] public DateOnly EnrolledOn { get; set; }
    public DateOnly? WithdrawnOn { get; set; }
    [Required] public Guid EnrolmentStatusId { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class EnrolmentListFilter
{
    public Guid? StudentId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? SchoolClassId { get; set; }
    public Guid? EnrolmentStatusId { get; set; }
}
