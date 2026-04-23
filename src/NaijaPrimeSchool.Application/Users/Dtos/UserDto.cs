namespace NaijaPrimeSchool.Application.Users.Dtos;

public class UserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string FullName { get; set; } = string.Empty;

    public Guid? TitleId { get; set; }
    public string? TitleName { get; set; }

    public Guid? GenderId { get; set; }
    public string? GenderName { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }

    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTimeOffset CreatedOn { get; set; }

    public List<string> Roles { get; set; } = [];
}
