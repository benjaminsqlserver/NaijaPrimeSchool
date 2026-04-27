using System.ComponentModel.DataAnnotations;

namespace NaijaPrimeSchool.Application.Academics.Dtos;

public class SubjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateSubjectRequest
{
    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(10)]
    public string Code { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }
}

public class UpdateSubjectRequest
{
    public Guid Id { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(10)]
    public string Code { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }
}
