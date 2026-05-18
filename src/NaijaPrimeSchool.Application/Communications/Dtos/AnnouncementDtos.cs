namespace NaijaPrimeSchool.Application.Communications.Dtos;

public class AnnouncementDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryCode { get; set; } = string.Empty;

    public Guid AudienceId { get; set; }
    public string AudienceName { get; set; } = string.Empty;
    public string AudienceCode { get; set; } = string.Empty;
    public bool AudienceRequiresTargetClass { get; set; }

    public Guid? TargetSchoolClassId { get; set; }
    public string? TargetSchoolClassName { get; set; }

    public Guid? PostedById { get; set; }
    public string? PostedByName { get; set; }

    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public bool IsPinned { get; set; }
    public bool IsExpired => ExpiresOn.HasValue && ExpiresOn.Value < DateOnly.FromDateTime(DateTime.UtcNow);

    public DateTimeOffset CreatedOn { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedOn { get; set; }
    public string? ModifiedBy { get; set; }

    public int ReadCount { get; set; }
    public bool ReadByCurrentUser { get; set; }
}

public class CreateAnnouncementRequest
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Guid AudienceId { get; set; }
    public Guid? TargetSchoolClassId { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public bool IsPinned { get; set; }
    public bool PublishImmediately { get; set; }
}

public class UpdateAnnouncementRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Guid AudienceId { get; set; }
    public Guid? TargetSchoolClassId { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public bool IsPinned { get; set; }
}

public class AnnouncementFilter
{
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? AudienceId { get; set; }
    public Guid? TargetSchoolClassId { get; set; }
    public bool? IsPublished { get; set; }
    public bool IncludeExpired { get; set; } = true;
}
