using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Family;

public class EnrolmentStatus : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public ICollection<Enrolment> Enrolments { get; set; } = [];
}
