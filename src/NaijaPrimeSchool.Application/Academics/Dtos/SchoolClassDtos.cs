using System.ComponentModel.DataAnnotations;

namespace NaijaPrimeSchool.Application.Academics.Dtos;

public class SchoolClassDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid ClassLevelId { get; set; }
    public string ClassLevelName { get; set; } = string.Empty;
    public Guid SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public Guid? ClassTeacherId { get; set; }
    public string? ClassTeacherName { get; set; }
}

public class CreateSchoolClassRequest
{
    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    [Required] public Guid ClassLevelId { get; set; }
    [Required] public Guid SessionId { get; set; }
    public Guid? ClassTeacherId { get; set; }
}

public class UpdateSchoolClassRequest
{
    public Guid Id { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    [Required] public Guid ClassLevelId { get; set; }
    [Required] public Guid SessionId { get; set; }
    public Guid? ClassTeacherId { get; set; }
}
