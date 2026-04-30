using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Family;

public class StudentParent : BaseEntity
{
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }

    public Guid ParentId { get; set; }
    public Parent? Parent { get; set; }

    public Guid RelationshipId { get; set; }
    public Relationship? Relationship { get; set; }

    public bool IsPrimaryContact { get; set; }
    public bool CanPickUp { get; set; } = true;
    public string? Notes { get; set; }
}
