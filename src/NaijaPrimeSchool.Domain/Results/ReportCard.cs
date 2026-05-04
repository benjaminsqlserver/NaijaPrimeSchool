using NaijaPrimeSchool.Domain.Academics;
using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Family;

namespace NaijaPrimeSchool.Domain.Results;

public class ReportCard : BaseEntity
{
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }

    public Guid TermId { get; set; }
    public Term? Term { get; set; }

    public Guid SchoolClassId { get; set; }
    public SchoolClass? SchoolClass { get; set; }

    public int SubjectsTaken { get; set; }
    public decimal TotalScore { get; set; }
    public decimal AveragePercentage { get; set; }

    public int? Position { get; set; }
    public int? StudentsInClass { get; set; }

    public int DaysPresent { get; set; }
    public int DaysAbsent { get; set; }
    public int DaysLate { get; set; }
    public int TotalSchoolDays { get; set; }

    public string? ClassTeacherComment { get; set; }
    public string? HeadTeacherComment { get; set; }

    public DateOnly? NextTermBegins { get; set; }

    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedOn { get; set; }

    public ICollection<AffectiveRating> AffectiveRatings { get; set; } = [];
    public ICollection<PsychomotorRating> PsychomotorRatings { get; set; } = [];
}
