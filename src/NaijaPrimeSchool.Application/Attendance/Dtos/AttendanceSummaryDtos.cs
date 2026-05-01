namespace NaijaPrimeSchool.Application.Attendance.Dtos;

public class StudentAttendanceSummaryDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentAdmissionNumber { get; set; } = string.Empty;

    public int DaysCounted { get; set; }
    public int DaysPresent { get; set; }
    public int DaysAbsent { get; set; }
    public int DaysLate { get; set; }
    public int DaysExcused { get; set; }

    public double PresentRate =>
        DaysCounted == 0 ? 0d : Math.Round(DaysPresent * 100d / DaysCounted, 1);
}

public class StudentAttendanceSummaryFilter
{
    public Guid StudentId { get; set; }
    public Guid? TermId { get; set; }
    public Guid? SessionId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public class ClassAttendanceSummaryDto
{
    public Guid SchoolClassId { get; set; }
    public string SchoolClassName { get; set; } = string.Empty;
    public List<StudentAttendanceSummaryDto> Students { get; set; } = [];
}
