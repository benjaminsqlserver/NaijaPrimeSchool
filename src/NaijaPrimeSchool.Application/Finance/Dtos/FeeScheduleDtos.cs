using System.ComponentModel.DataAnnotations;

namespace NaijaPrimeSchool.Application.Finance.Dtos;

public class FeeScheduleDto
{
    public Guid Id { get; set; }

    public Guid TermId { get; set; }
    public string TermName { get; set; } = string.Empty;

    public Guid SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;

    public Guid ClassLevelId { get; set; }
    public string ClassLevelName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedOn { get; set; }

    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
    public int InvoicesIssued { get; set; }
}

public class FeeScheduleItemDto
{
    public Guid Id { get; set; }
    public Guid FeeScheduleId { get; set; }
    public Guid FeeCategoryId { get; set; }
    public string FeeCategoryName { get; set; } = string.Empty;
    public string FeeCategoryCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsMandatory { get; set; }
    public int DisplayOrder { get; set; }
}

public class FeeScheduleDetailDto
{
    public FeeScheduleDto Schedule { get; set; } = new();
    public List<FeeScheduleItemDto> Items { get; set; } = [];
}

public class CreateFeeScheduleRequest
{
    [Required] public Guid TermId { get; set; }
    [Required] public Guid ClassLevelId { get; set; }

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class UpdateFeeScheduleRequest
{
    public Guid Id { get; set; }

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class UpsertFeeScheduleItemRequest
{
    public Guid? Id { get; set; }
    [Required] public Guid FeeScheduleId { get; set; }
    [Required] public Guid FeeCategoryId { get; set; }

    [Required, StringLength(160)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 100000000.0)]
    public decimal Amount { get; set; }

    public bool IsMandatory { get; set; } = true;
    public int DisplayOrder { get; set; }
}

public class FeeScheduleFilter
{
    public Guid? TermId { get; set; }
    public Guid? ClassLevelId { get; set; }
    public bool? IsPublished { get; set; }
}
