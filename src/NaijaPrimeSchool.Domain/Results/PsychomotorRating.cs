using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Results;

public class PsychomotorRating : BaseEntity
{
    public Guid ReportCardId { get; set; }
    public ReportCard? ReportCard { get; set; }

    public Guid PsychomotorSkillId { get; set; }
    public PsychomotorSkill? PsychomotorSkill { get; set; }

    public Guid TraitRatingId { get; set; }
    public TraitRating? TraitRating { get; set; }
}
