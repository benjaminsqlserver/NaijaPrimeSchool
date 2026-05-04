using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Results;

public class AffectiveRating : BaseEntity
{
    public Guid ReportCardId { get; set; }
    public ReportCard? ReportCard { get; set; }

    public Guid AffectiveTraitId { get; set; }
    public AffectiveTrait? AffectiveTrait { get; set; }

    public Guid TraitRatingId { get; set; }
    public TraitRating? TraitRating { get; set; }
}
