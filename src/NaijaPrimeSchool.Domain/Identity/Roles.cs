namespace NaijaPrimeSchool.Domain.Identity;

public static class Roles
{
    public const string SuperAdmin = nameof(SuperAdmin);
    public const string HeadTeacher = nameof(HeadTeacher);
    public const string Teacher = nameof(Teacher);
    public const string SchoolBursar = nameof(SchoolBursar);
    public const string SchoolStoreKeeper = nameof(SchoolStoreKeeper);
    public const string Parent = nameof(Parent);
    public const string Student = nameof(Student);

    public static readonly IReadOnlyList<string> All =
    [
        SuperAdmin,
        HeadTeacher,
        Teacher,
        SchoolBursar,
        SchoolStoreKeeper,
        Parent,
        Student,
    ];
}
