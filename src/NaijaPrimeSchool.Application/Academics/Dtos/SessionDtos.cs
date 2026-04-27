using System.ComponentModel.DataAnnotations;

namespace NaijaPrimeSchool.Application.Academics.Dtos;

public class SessionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public int TermCount { get; set; }
    public int ClassCount { get; set; }
}

public class CreateSessionRequest
{
    [Required, StringLength(40)]
    public string Name { get; set; } = string.Empty;

    [Required] public DateOnly StartDate { get; set; }
    [Required] public DateOnly EndDate { get; set; }

    public bool IsCurrent { get; set; }
}

public class UpdateSessionRequest
{
    public Guid Id { get; set; }

    [Required, StringLength(40)]
    public string Name { get; set; } = string.Empty;

    [Required] public DateOnly StartDate { get; set; }
    [Required] public DateOnly EndDate { get; set; }

    public bool IsCurrent { get; set; }
}
