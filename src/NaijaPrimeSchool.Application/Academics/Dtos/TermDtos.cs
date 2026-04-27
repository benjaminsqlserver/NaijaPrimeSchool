using System.ComponentModel.DataAnnotations;

namespace NaijaPrimeSchool.Application.Academics.Dtos;

public class TermDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public Guid TermTypeId { get; set; }
    public string TermTypeName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsCurrent { get; set; }
}

public class CreateTermRequest
{
    [Required] public Guid SessionId { get; set; }
    [Required] public Guid TermTypeId { get; set; }
    [Required] public DateOnly StartDate { get; set; }
    [Required] public DateOnly EndDate { get; set; }
    public bool IsCurrent { get; set; }
}

public class UpdateTermRequest
{
    public Guid Id { get; set; }
    [Required] public Guid SessionId { get; set; }
    [Required] public Guid TermTypeId { get; set; }
    [Required] public DateOnly StartDate { get; set; }
    [Required] public DateOnly EndDate { get; set; }
    public bool IsCurrent { get; set; }
}
