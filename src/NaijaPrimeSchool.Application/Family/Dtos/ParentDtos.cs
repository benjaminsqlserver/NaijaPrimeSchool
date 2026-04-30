using System.ComponentModel.DataAnnotations;

namespace NaijaPrimeSchool.Application.Family.Dtos;

public class ParentDto
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string FullName { get; set; } = string.Empty;

    public Guid? TitleId { get; set; }
    public string? TitleName { get; set; }

    public Guid? GenderId { get; set; }
    public string? GenderName { get; set; }

    public Guid? MaritalStatusId { get; set; }
    public string? MaritalStatusName { get; set; }

    public string? PrimaryPhone { get; set; }
    public string? AlternatePhone { get; set; }
    public string? Email { get; set; }
    public string? ResidentialAddress { get; set; }
    public string? Occupation { get; set; }
    public string? Employer { get; set; }

    public bool IsActive { get; set; }

    public int StudentCount { get; set; }
}

public class CreateParentRequest
{
    [Required, StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(80)]
    public string? MiddleName { get; set; }

    public Guid? TitleId { get; set; }
    public Guid? GenderId { get; set; }
    public Guid? MaritalStatusId { get; set; }

    [Phone, StringLength(30)]
    public string? PrimaryPhone { get; set; }

    [Phone, StringLength(30)]
    public string? AlternatePhone { get; set; }

    [EmailAddress, StringLength(256)]
    public string? Email { get; set; }

    [StringLength(300)]
    public string? ResidentialAddress { get; set; }

    [StringLength(120)]
    public string? Occupation { get; set; }

    [StringLength(120)]
    public string? Employer { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateParentRequest
{
    public Guid Id { get; set; }

    [Required, StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(80)]
    public string? MiddleName { get; set; }

    public Guid? TitleId { get; set; }
    public Guid? GenderId { get; set; }
    public Guid? MaritalStatusId { get; set; }

    [Phone, StringLength(30)]
    public string? PrimaryPhone { get; set; }

    [Phone, StringLength(30)]
    public string? AlternatePhone { get; set; }

    [EmailAddress, StringLength(256)]
    public string? Email { get; set; }

    [StringLength(300)]
    public string? ResidentialAddress { get; set; }

    [StringLength(120)]
    public string? Occupation { get; set; }

    [StringLength(120)]
    public string? Employer { get; set; }
}

public class ParentListFilter
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; } = 20;
    public string? OrderBy { get; set; }
}
