using System.ComponentModel.DataAnnotations;

namespace NaijaPrimeSchool.Application.Family.Dtos;

public class StudentParentDto
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentAdmissionNumber { get; set; } = string.Empty;

    public Guid ParentId { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public string? ParentPhone { get; set; }
    public string? ParentEmail { get; set; }

    public Guid RelationshipId { get; set; }
    public string RelationshipName { get; set; } = string.Empty;

    public bool IsPrimaryContact { get; set; }
    public bool CanPickUp { get; set; }
    public string? Notes { get; set; }
}

public class LinkStudentParentRequest
{
    [Required] public Guid StudentId { get; set; }
    [Required] public Guid ParentId { get; set; }
    [Required] public Guid RelationshipId { get; set; }
    public bool IsPrimaryContact { get; set; }
    public bool CanPickUp { get; set; } = true;

    [StringLength(300)]
    public string? Notes { get; set; }
}

public class UpdateStudentParentLinkRequest
{
    public Guid Id { get; set; }
    [Required] public Guid RelationshipId { get; set; }
    public bool IsPrimaryContact { get; set; }
    public bool CanPickUp { get; set; } = true;

    [StringLength(300)]
    public string? Notes { get; set; }
}
