using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Finance;

public class FeeScheduleItem : BaseEntity
{
    public Guid FeeScheduleId { get; set; }
    public FeeSchedule? FeeSchedule { get; set; }

    public Guid FeeCategoryId { get; set; }
    public FeeCategory? FeeCategory { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    public bool IsMandatory { get; set; } = true;
    public int DisplayOrder { get; set; }
}
