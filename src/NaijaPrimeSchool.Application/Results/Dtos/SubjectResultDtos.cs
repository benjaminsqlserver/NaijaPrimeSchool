namespace NaijaPrimeSchool.Application.Results.Dtos;

public class SubjectResultDto
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentAdmissionNumber { get; set; } = string.Empty;

    public Guid TermId { get; set; }
    public string TermName { get; set; } = string.Empty;

    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;

    public Guid SchoolClassId { get; set; }
    public string SchoolClassName { get; set; } = string.Empty;

    public decimal TotalScore { get; set; }
    public decimal Percentage { get; set; }

    public Guid? GradeBandId { get; set; }
    public string? GradeBandName { get; set; }
    public string? GradeBandRemark { get; set; }

    public int? Position { get; set; }
    public int? StudentsInClass { get; set; }

    public string? TeacherComment { get; set; }
    public bool IsFinalised { get; set; }
    public DateTimeOffset? FinalisedOn { get; set; }
}

public class SubjectResultFilter
{
    public Guid? StudentId { get; set; }
    public Guid? TermId { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? SchoolClassId { get; set; }
    public bool? IsFinalised { get; set; }
}

public class ComputeResultsRequest
{
    public Guid TermId { get; set; }
    public Guid SchoolClassId { get; set; }
    public Guid? SubjectId { get; set; }
    public bool Finalise { get; set; }
}

public class ComputeResultsResponse
{
    public int RowsComputed { get; set; }
    public int RowsFinalised { get; set; }
    public List<string> Warnings { get; set; } = [];
}

public class UpdateSubjectResultRequest
{
    public Guid Id { get; set; }

    public string? TeacherComment { get; set; }
}
