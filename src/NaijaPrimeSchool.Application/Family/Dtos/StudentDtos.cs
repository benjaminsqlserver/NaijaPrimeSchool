using System.ComponentModel.DataAnnotations;

namespace NaijaPrimeSchool.Application.Family.Dtos;

public class StudentDto
{
    public Guid Id { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty;
    public DateOnly AdmissionDate { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string FullName { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public Guid? GenderId { get; set; }
    public string? GenderName { get; set; }

    public Guid? BloodGroupId { get; set; }
    public string? BloodGroupName { get; set; }

    public string? StateOfOrigin { get; set; }
    public string? ResidentialAddress { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Allergies { get; set; }
    public string? MedicalNotes { get; set; }

    public bool IsActive { get; set; }

    public Guid? CurrentClassId { get; set; }
    public string? CurrentClassName { get; set; }
    public Guid? CurrentSessionId { get; set; }
    public string? CurrentSessionName { get; set; }

    public int ParentCount { get; set; }
    public string? PrimaryContactName { get; set; }
    public string? PrimaryContactPhone { get; set; }
}

public class CreateStudentRequest
{
    [Required, StringLength(30)]
    public string AdmissionNumber { get; set; } = string.Empty;

    [Required] public DateOnly AdmissionDate { get; set; }

    [Required, StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(80)]
    public string? MiddleName { get; set; }

    [Required] public DateOnly DateOfBirth { get; set; }

    public Guid? GenderId { get; set; }
    public Guid? BloodGroupId { get; set; }

    [StringLength(80)]
    public string? StateOfOrigin { get; set; }

    [StringLength(300)]
    public string? ResidentialAddress { get; set; }

    [StringLength(500)]
    public string? PhotoUrl { get; set; }

    [StringLength(500)]
    public string? Allergies { get; set; }

    [StringLength(1000)]
    public string? MedicalNotes { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid? InitialClassId { get; set; }
}

public class UpdateStudentRequest
{
    public Guid Id { get; set; }

    [Required, StringLength(30)]
    public string AdmissionNumber { get; set; } = string.Empty;

    [Required] public DateOnly AdmissionDate { get; set; }

    [Required, StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(80)]
    public string? MiddleName { get; set; }

    [Required] public DateOnly DateOfBirth { get; set; }

    public Guid? GenderId { get; set; }
    public Guid? BloodGroupId { get; set; }

    [StringLength(80)]
    public string? StateOfOrigin { get; set; }

    [StringLength(300)]
    public string? ResidentialAddress { get; set; }

    [StringLength(500)]
    public string? PhotoUrl { get; set; }

    [StringLength(500)]
    public string? Allergies { get; set; }

    [StringLength(1000)]
    public string? MedicalNotes { get; set; }
}

public class StudentListFilter
{
    public string? SearchTerm { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? SessionId { get; set; }
    public bool? IsActive { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; } = 20;
    public string? OrderBy { get; set; }
}
