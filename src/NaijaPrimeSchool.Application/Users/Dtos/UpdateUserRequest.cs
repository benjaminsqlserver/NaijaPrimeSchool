using System.ComponentModel.DataAnnotations;

namespace NaijaPrimeSchool.Application.Users.Dtos;

public class UpdateUserRequest
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

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }
}
