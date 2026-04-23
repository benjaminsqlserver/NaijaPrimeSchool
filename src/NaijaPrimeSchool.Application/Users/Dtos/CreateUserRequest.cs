using System.ComponentModel.DataAnnotations;

namespace NaijaPrimeSchool.Application.Users.Dtos;

public class CreateUserRequest
{
    [Required, StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(80)]
    public string? MiddleName { get; set; }

    public Guid? TitleId { get; set; }

    public Guid? GenderId { get; set; }

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(64, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [Required, StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please assign at least one role")]
    [MinLength(1, ErrorMessage = "Please assign at least one role")]
    public List<string> Roles { get; set; } = [];

    public bool IsActive { get; set; } = true;
}
