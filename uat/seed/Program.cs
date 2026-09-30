// Seeds "Naija Prime School" with realistic demo data for User Acceptance
// Testing. Everything goes through the application's own services (the same
// ones the UI calls), acting as the appropriate staff member, so business
// rules, validation and the audit trail all apply exactly as in real use.
//
//   dotnet run --project uat/seed -- "<connection string>" [--reset]
//
// --reset drops and recreates the database first. For safety it is refused
// unless the database name contains "UAT".
//
// All dates are relative to the day the seeder runs, so the demo school always
// looks current: the academic session is the one containing today, the
// current term is the one containing today, and attendance covers the last
// ten school days.

using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NaijaPrimeSchool.Application.Academics;
using NaijaPrimeSchool.Application.Academics.Dtos;
using NaijaPrimeSchool.Application.Attendance;
using NaijaPrimeSchool.Application.Attendance.Dtos;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Communications;
using NaijaPrimeSchool.Application.Communications.Dtos;
using NaijaPrimeSchool.Application.Family;
using NaijaPrimeSchool.Application.Family.Dtos;
using NaijaPrimeSchool.Application.Finance;
using NaijaPrimeSchool.Application.Finance.Dtos;
using NaijaPrimeSchool.Application.Inventory;
using NaijaPrimeSchool.Application.Inventory.Dtos;
using NaijaPrimeSchool.Application.Messaging;
using NaijaPrimeSchool.Application.Messaging.Dtos;
using NaijaPrimeSchool.Application.Results;
using NaijaPrimeSchool.Application.Results.Dtos;
using NaijaPrimeSchool.Application.Users;
using NaijaPrimeSchool.Application.Users.Dtos;
using NaijaPrimeSchool.Domain.Identity;
using NaijaPrimeSchool.Infrastructure;
using NaijaPrimeSchool.Infrastructure.Persistence;
using NaijaPrimeSchool.UatSeed;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: dotnet run --project uat/seed -- \"<connection string>\" [--reset]");
    return 2;
}

var connectionString = args[0];
var reset = args.Contains("--reset");
var dbName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
if (reset && !dbName.Contains("UAT", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine($"Refusing to reset '{dbName}': --reset only works on databases whose name contains 'UAT'.");
    return 2;
}

var actor = new SeedActor();
var services = new ServiceCollection();
services.AddDataProtection();
services.AddLogging(b => b.AddSimpleConsole(o => o.SingleLine = true).SetMinimumLevel(LogLevel.Warning));
services.AddSingleton<ICurrentUser>(actor);
services.AddHttpContextAccessor();
services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:DefaultConnection"] = connectionString,
    ["OnlinePayments:Provider"] = "Simulator",
}).Build());
await using var root = services.BuildServiceProvider();

if (reset)
{
    await using var scope = root.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    Console.WriteLine($"Dropping database {dbName}...");
    await db.Database.EnsureDeletedAsync();
}

await DatabaseInitializer.InitializeAsync(root);
var seed = new Seeder(root, actor);
await seed.RunAsync();
return 0;

namespace NaijaPrimeSchool.UatSeed
{
    // The "signed-in user" while seeding; switched per section so records
    // (and the audit trail) are attributed to the right staff member.
    public sealed class SeedActor : ICurrentUser
    {
        public Guid? UserId { get; set; }
        public string? UserName { get; set; }
        public bool IsAuthenticated => UserId is not null;
        public IReadOnlyList<string> Roles { get; set; } = [];
        public bool IsInRole(string role) => Roles.Contains(role);
    }

    public sealed class Seeder(IServiceProvider root, SeedActor actor)
    {
        public const string StaffPassword = "Uat@12345";
        public const string FamilyPassword = "Family@123";

        private readonly Random rng = new(20260930);
        private readonly Dictionary<string, Guid> staff = new();   // username -> user id
        private readonly Dictionary<string, Guid> classes = new(); // "Primary 5A" -> class id
        private readonly Dictionary<string, Guid> subjects = new(); // code -> subject id
        private readonly Dictionary<string, List<Guid>> pupilsByClass = new();
        private readonly Dictionary<Guid, Guid> parentOfPupil = new();
        private readonly List<(Guid ParentId, Guid UserId, string Name)> parents = [];
        private Guid sessionId, currentTermId, previousSessionId;
        private DateOnly termStart, termEnd, today;

        private T Svc<T>(IServiceScope s) where T : notnull => s.ServiceProvider.GetRequiredService<T>();

        private async Task<TResult> As<TResult>(string userName, string[] roles, Func<IServiceScope, Task<TResult>> work)
        {
            actor.UserName = userName;
            actor.UserId = userName == "system" ? null : staff.GetValueOrDefault(userName);
            actor.Roles = roles;
            await using var scope = root.CreateAsyncScope();
            return await work(scope);
        }

        private static T Ok<T>(OperationResult<T> r, string what) =>
            r.Succeeded ? r.Data! : throw new InvalidOperationException($"{what} failed: {string.Join("; ", r.Errors)}");

        private static void Ok(OperationResult r, string what)
        {
            if (!r.Succeeded) throw new InvalidOperationException($"{what} failed: {string.Join("; ", r.Errors)}");
        }

        private static void Step(string text) => Console.WriteLine($"  • {text}");

        public async Task RunAsync()
        {
            today = DateOnly.FromDateTime(DateTime.Today);
            Console.WriteLine($"Seeding Naija Prime School demo data (today = {today:dd MMM yyyy})");

            await SeedStaffAsync();
            await SeedAcademicsAsync();
            await SeedFamiliesAsync();
            await SeedAttendanceAsync();
            await SeedResultsAsync();
            await SeedFinanceAsync();
            await SeedInventoryAsync();
            await SeedCommunicationsAsync();
            await SeedMessagingAsync();

            Console.WriteLine("Done. Sign-in details are in uat/README.md.");
        }

        // ------------------------------------------------------------------ staff
        private async Task SeedStaffAsync()
        {
            Console.WriteLine("Staff accounts");
            staff["superadmin"] = await As("system", [Roles.SuperAdmin], async s =>
                await Svc<ApplicationDbContext>(s).Users.Where(u => u.UserName == "superadmin").Select(u => u.Id).FirstAsync());

            (string User, string First, string Last, string Title, string Gender, string Role, string Phone)[] people =
            [
                ("folake.adeyemi", "Folake", "Adeyemi", "Mrs.", "F", Roles.HeadTeacher, "08031110001"),
                ("chinedu.okeke", "Chinedu", "Okeke", "Mr.", "M", Roles.Teacher, "08031110002"),
                ("aisha.bello", "Aisha", "Bello", "Miss", "F", Roles.Teacher, "08031110003"),
                ("emeka.nwosu", "Emeka", "Nwosu", "Mr.", "M", Roles.Teacher, "08031110004"),
                ("grace.udoh", "Grace", "Udoh", "Mrs.", "F", Roles.Teacher, "08031110005"),
                ("tunde.bakare", "Tunde", "Bakare", "Mr.", "M", Roles.SchoolBursar, "08031110006"),
                ("musa.ibrahim", "Musa", "Ibrahim", "Mr.", "M", Roles.SchoolStoreKeeper, "08031110007"),
            ];

            await As("superadmin", [Roles.SuperAdmin], async s =>
            {
                var db = Svc<ApplicationDbContext>(s);
                var titles = await db.Titles.ToDictionaryAsync(t => t.Name, t => t.Id);
                var genders = await db.Genders.ToDictionaryAsync(g => g.Code, g => g.Id);
                foreach (var p in people)
                {
                    var id = Ok(await Svc<IUserService>(s).CreateAsync(new CreateUserRequest
                    {
                        FirstName = p.First, LastName = p.Last, UserName = p.User,
                        Email = $"{p.User}@naijaprimeschool.ng", PhoneNumber = p.Phone,
                        TitleId = titles[p.Title], GenderId = genders[p.Gender],
                        Password = StaffPassword, ConfirmPassword = StaffPassword, Roles = [p.Role],
                    }), $"create user {p.User}");
                    staff[p.User] = id;
                }
                return 0;
            });
            Step($"{people.Length} staff: head teacher, 4 teachers, bursar, storekeeper (password {StaffPassword})");
        }

        // -------------------------------------------------------------- academics
        private async Task SeedAcademicsAsync()
        {
            Console.WriteLine("Academic structure");
            var y = today.Month >= 9 ? today.Year : today.Year - 1;
            var head = new[] { Roles.HeadTeacher };

            await As("folake.adeyemi", head, async s =>
            {
                var db = Svc<ApplicationDbContext>(s);
                var termTypes = await db.TermTypes.OrderBy(t => t.DisplayOrder).Select(t => t.Id).ToListAsync();
                var levels = await db.ClassLevels.ToDictionaryAsync(l => l.Name, l => l.Id);
                var sessions = Svc<ISessionService>(s);
                var terms = Svc<ITermService>(s);

                // Last year, for history.
                previousSessionId = Ok(await sessions.CreateAsync(new CreateSessionRequest
                { Name = $"{y - 1}/{y}", StartDate = new(y - 1, 9, 1), EndDate = new(y, 7, 31) }), "previous session");
                sessionId = Ok(await sessions.CreateAsync(new CreateSessionRequest
                { Name = $"{y}/{y + 1}", StartDate = new(y, 9, 1), EndDate = new(y + 1, 7, 31), IsCurrent = true }), "session");

                (DateOnly Start, DateOnly End)[] termDates =
                [
                    (new(y, 9, 8), new(y, 12, 18)),
                    (new(y + 1, 1, 11), new(y + 1, 4, 1)),
                    (new(y + 1, 4, 26), new(y + 1, 7, 23)),
                ];
                var currentIndex = Array.FindIndex(termDates, t => today >= t.Start && today <= t.End);
                if (currentIndex < 0) currentIndex = today < termDates[1].Start ? 0 : today < termDates[2].Start ? 1 : 2;
                for (var i = 0; i < 3; i++)
                {
                    var id = Ok(await terms.CreateAsync(new CreateTermRequest
                    {
                        SessionId = sessionId, TermTypeId = termTypes[i],
                        StartDate = termDates[i].Start, EndDate = termDates[i].End, IsCurrent = i == currentIndex,
                    }), $"term {i + 1}");
                    if (i == currentIndex) { currentTermId = id; (termStart, termEnd) = termDates[i]; }
                }

                (string Code, string Name)[] subjectList =
                [
                    ("ENG", "English Language"), ("MTH", "Mathematics"), ("BSC", "Basic Science"),
                    ("SST", "Social Studies"), ("CRS", "Christian Religious Studies"), ("YOR", "Yoruba"),
                    ("CMP", "Computer Studies"), ("CCA", "Cultural & Creative Arts"), ("PHE", "Physical & Health Education"),
                ];
                foreach (var sub in subjectList)
                    subjects[sub.Code] = Ok(await Svc<ISubjectService>(s).CreateAsync(new CreateSubjectRequest { Code = sub.Code, Name = sub.Name }), $"subject {sub.Code}");

                (string Name, string Level, string? Teacher)[] classList =
                [
                    ("Primary 1A", "Primary 1", "aisha.bello"),
                    ("Primary 2A", "Primary 2", "grace.udoh"),
                    ("Primary 3A", "Primary 3", null),
                    ("Primary 4A", "Primary 4", "emeka.nwosu"),
                    ("Primary 5A", "Primary 5", "chinedu.okeke"),
                    ("Primary 5B", "Primary 5", null),
                    ("Primary 6A", "Primary 6", null),
                ];
                foreach (var c in classList)
                {
                    classes[c.Name] = Ok(await Svc<ISchoolClassService>(s).CreateAsync(new CreateSchoolClassRequest
                    {
                        Name = c.Name, ClassLevelId = levels[c.Level], SessionId = sessionId,
                        ClassTeacherId = c.Teacher is null ? null : staff[c.Teacher],
                    }), $"class {c.Name}");
                }

                // Weekly timetable for Primary 5A (every period) and part of Primary 1A.
                var days = await db.WeekDays.OrderBy(d => d.DisplayOrder).Select(d => d.Id).ToListAsync();
                var periods = await db.TimetablePeriods.Where(p => !p.IsBreak).OrderBy(p => p.DisplayOrder).Select(p => p.Id).ToListAsync();
                string[] p5Rotation = ["ENG", "MTH", "BSC", "SST", "CRS", "YOR", "CMP", "CCA", "PHE"];
                var tt = Svc<ITimetableService>(s);
                for (var d = 0; d < days.Count; d++)
                {
                    for (var p = 0; p < periods.Count; p++)
                    {
                        var code = p5Rotation[(d * 3 + p) % p5Rotation.Length];
                        Ok(await tt.UpsertEntryAsync(new UpsertTimetableEntryRequest
                        {
                            TermId = currentTermId, SchoolClassId = classes["Primary 5A"], SubjectId = subjects[code],
                            WeekDayId = days[d], TimetablePeriodId = periods[p],
                            TeacherId = code is "MTH" or "BSC" or "CMP" ? staff["chinedu.okeke"] : staff["emeka.nwosu"],
                            Room = "Block B, Room 5",
                        }), "P5A timetable");
                    }
                    for (var p = 0; p < 3; p++)
                    {
                        Ok(await tt.UpsertEntryAsync(new UpsertTimetableEntryRequest
                        {
                            TermId = currentTermId, SchoolClassId = classes["Primary 1A"], SubjectId = subjects[new[] { "ENG", "MTH", "CCA" }[p]],
                            WeekDayId = days[d], TimetablePeriodId = periods[p], TeacherId = staff["aisha.bello"], Room = "Block A, Room 1",
                        }), "P1A timetable");
                    }
                }
                return 0;
            });
            Step($"sessions {y - 1}/{y} and {y}/{y + 1} (current), 3 terms, current term {termStart:dd MMM}–{termEnd:dd MMM yyyy}");
            Step($"{subjects.Count} subjects, {classes.Count} classes, full weekly timetable for Primary 5A, part of Primary 1A");
        }

        // --------------------------------------------------------------- families
        private static readonly (string Surname, string Father, string Mother, string[] Kids)[] Families =
        [
            ("Okafor", "Emeka", "Ngozi", ["Chiamaka", "Tobenna"]),
            ("Adebayo", "Olumide", "Funke", ["Tolu", "Ayomide"]),
            ("Bello", "Abubakar", "Hauwa", ["Zainab"]),
            ("Eze", "Chukwudi", "Adaeze", ["Kamsi", "Obinna"]),
            ("Balogun", "Kunle", "Yetunde", ["Damilola"]),
            ("Ibrahim", "Sani", "Maryam", ["Aminu", "Fatima"]),
            ("Nwachukwu", "Ifeanyi", "Chioma", ["Ebuka"]),
            ("Ogunleye", "Segun", "Bisi", ["Temitope", "Ireoluwa"]),
            ("Uche", "Nnamdi", "Amaka", ["Chidera"]),
            ("Lawal", "Rasheed", "Kemi", ["Aisha", "Yusuf"]),
            ("Okon", "Effiong", "Imaobong", ["Ekaette"]),
            ("Danjuma", "Yakubu", "Ladi", ["Musa"]),
            ("Akinola", "Tayo", "Ronke", ["Feyisara", "Oluwaseun"]),
            ("Obi", "Chinedu", "Nkechi", ["Somto"]),
            ("Salami", "Wale", "Shade", ["Kehinde", "Taiwo"]),
            ("Nwosu", "Ikenna", "Ogechi", ["Chisom"]),
            ("Yusuf", "Hassan", "Zainab", ["Halima"]),
            ("Adeleke", "Bayo", "Tope", ["Morenike", "Opeyemi"]),
            ("Ekwueme", "Obiora", "Uju", ["Nnenna"]),
            ("Martins", "Victor", "Joy", ["Precious", "Daniel"]),
        ];

        private async Task SeedFamiliesAsync()
        {
            Console.WriteLine("Pupils and parents");
            // Where each child goes: Primary 5A and Primary 1A get the most pupils
            // (they carry the timetable, registers, results and fees in the demo).
            string[] placement =
            [
                "Primary 5A", "Primary 1A", "Primary 5A", "Primary 2A", "Primary 5A", "Primary 5A", "Primary 1A",
                "Primary 4A", "Primary 5A", "Primary 3A", "Primary 5A", "Primary 1A", "Primary 5A", "Primary 6A",
                "Primary 5A", "Primary 1A", "Primary 5A", "Primary 1A", "Primary 5B", "Primary 5A", "Primary 1A",
                "Primary 5A", "Primary 2A", "Primary 1A", "Primary 5A", "Primary 6A", "Primary 1A", "Primary 5B",
                "Primary 5A", "Primary 4A", "Primary 1A", "Primary 5A", "Primary 3A",
            ];
            var slot = 0;
            var admission = 1;

            await As("folake.adeyemi", [Roles.HeadTeacher], async s =>
            {
                var db = Svc<ApplicationDbContext>(s);
                var genders = await db.Genders.ToDictionaryAsync(g => g.Code, g => g.Id);
                var titles = await db.Titles.ToDictionaryAsync(t => t.Name, t => t.Id);
                var relationships = await db.Relationships.ToDictionaryAsync(r => r.Name, r => r.Id);
                var blood = await db.BloodGroups.Select(b => b.Id).ToListAsync();
                var studentSvc = Svc<IStudentService>(s);
                var parentSvc = Svc<IParentService>(s);

                foreach (var (fam, i) in Families.Select((f, i) => (f, i)))
                {
                    // Mostly mothers as primary contact; a few fathers.
                    var mother = i % 4 != 3;
                    var first = mother ? fam.Mother : fam.Father;
                    var user = $"{first.ToLowerInvariant()}.{fam.Surname.ToLowerInvariant()}";
                    var phone = $"080{(35000000 + i * 1011):D8}";
                    var parentId = Ok(await parentSvc.CreateAsync(new CreateParentRequest
                    {
                        FirstName = first, LastName = fam.Surname,
                        TitleId = titles[mother ? "Mrs." : "Mr."],
                        GenderId = genders[mother ? "F" : "M"],
                        PrimaryPhone = phone, Email = $"{user}@example.ng",
                        ResidentialAddress = $"{10 + i} {new[] { "Allen Avenue, Ikeja", "Adeola Odeku, Victoria Island", "Awolowo Road, Ikoyi", "Ogui Road, Enugu", "Wuse 2, Abuja" }[i % 5]}",
                        Occupation = new[] { "Banker", "Trader", "Engineer", "Nurse", "Civil servant", "Lawyer", "Teacher", "Pharmacist" }[i % 8],
                        UserName = user, Password = FamilyPassword,
                    }), $"parent {user}");
                    var parentUser = await db.Parents.Where(p => p.Id == parentId).Select(p => p.UserId!.Value).FirstAsync();
                    parents.Add((parentId, parentUser, $"{first} {fam.Surname}"));

                    foreach (var kid in fam.Kids)
                    {
                        var cls = placement[slot++ % placement.Length];
                        var level = int.Parse(cls.Split(' ')[1][..1]);
                        var dob = new DateOnly(today.Year - 5 - level, 1 + rng.Next(12), 1 + rng.Next(28));
                        var girl = kid is "Chiamaka" or "Tolu" or "Ayomide" or "Zainab" or "Kamsi" or "Damilola" or "Fatima" or "Temitope"
                            or "Ireoluwa" or "Chidera" or "Aisha" or "Ekaette" or "Feyisara" or "Somto" or "Kehinde" or "Chisom" or "Halima"
                            or "Morenike" or "Nnenna" or "Precious";
                        var studentUser = $"{kid.ToLowerInvariant()}.{fam.Surname.ToLowerInvariant()}";
                        var studentId = Ok(await studentSvc.CreateAsync(new CreateStudentRequest
                        {
                            AdmissionNumber = $"NPS/{today.Year % 100:D2}/{admission++:D3}",
                            AdmissionDate = new(termStart.Year - (level - 1), 9, 8),
                            FirstName = kid, LastName = fam.Surname, DateOfBirth = dob,
                            GenderId = genders[girl ? "F" : "M"],
                            BloodGroupId = blood[rng.Next(blood.Count)],
                            StateOfOrigin = new[] { "Lagos", "Anambra", "Kano", "Enugu", "Oyo", "Rivers", "Kaduna", "Imo" }[i % 8],
                            ResidentialAddress = $"{10 + i} {new[] { "Allen Avenue, Ikeja", "Adeola Odeku, Victoria Island", "Awolowo Road, Ikoyi", "Ogui Road, Enugu", "Wuse 2, Abuja" }[i % 5]}",
                            Allergies = kid is "Zainab" or "Somto" ? "Peanuts" : null,
                            InitialClassId = classes[cls],
                            UserName = studentUser, Email = $"{studentUser}@students.naijaprimeschool.ng", Password = FamilyPassword,
                        }), $"student {studentUser}");
                        pupilsByClass.TryAdd(cls, []);
                        pupilsByClass[cls].Add(studentId);
                        parentOfPupil[studentId] = parentId;

                        Ok(await studentSvc.LinkParentAsync(new LinkStudentParentRequest
                        {
                            StudentId = studentId, ParentId = parentId,
                            RelationshipId = relationships[mother ? "Mother" : "Father"], IsPrimaryContact = true,
                        }), $"link {studentUser}");
                    }
                }
                return 0;
            });
            Step($"{parents.Count} parents and {pupilsByClass.Values.Sum(v => v.Count)} pupils, all with portal accounts (password {FamilyPassword}), linked and enrolled");
            Step("per class: " + string.Join(", ", pupilsByClass.OrderBy(k => k.Key).Select(k => $"{k.Key} {k.Value.Count}")));
        }

        // ------------------------------------------------------------- attendance
        private async Task SeedAttendanceAsync()
        {
            Console.WriteLine("Attendance");
            // Last ten school days before today, inside the current term.
            var days = new List<DateOnly>();
            for (var d = today.AddDays(-1); days.Count < 10 && d >= termStart; d = d.AddDays(-1))
                if (d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) days.Add(d);
            days.Reverse();

            foreach (var (cls, teacher) in new[] { ("Primary 5A", "chinedu.okeke"), ("Primary 1A", "aisha.bello") })
            {
                await As(teacher, [Roles.Teacher], async s =>
                {
                    var db = Svc<ApplicationDbContext>(s);
                    var status = await db.AttendanceStatuses.ToDictionaryAsync(a => a.Code, a => a.Id);
                    var svc = Svc<IDailyAttendanceService>(s);
                    foreach (var day in days)
                    {
                        var registerId = Ok(await svc.OpenAsync(new OpenDailyRegisterRequest { SchoolClassId = classes[cls], Date = day, TakenById = staff[teacher] }), "open register");
                        var entries = pupilsByClass[cls].Select(pupil =>
                        {
                            var roll = rng.Next(100);
                            var code = roll < 86 ? "P" : roll < 92 ? "L" : roll < 96 ? "A" : roll < 98 ? "S" : "E";
                            return new UpsertDailyEntryRequest
                            {
                                RegisterId = registerId, StudentId = pupil, AttendanceStatusId = status[code],
                                ArrivalTime = code == "L" ? new TimeOnly(8, 10 + rng.Next(30)) : null,
                                Remarks = code == "S" ? "Malaria, parent called" : null,
                            };
                        }).ToList();
                        Ok(await svc.BulkSetAsync(new BulkSetDailyAttendanceRequest { RegisterId = registerId, Entries = entries }), "mark register");
                        Ok(await svc.SubmitAsync(registerId), "submit register");
                    }
                    return 0;
                });
            }
            Step($"submitted daily registers for Primary 5A and Primary 1A on {days.Count} school days ({days.First():dd MMM}–{days.Last():dd MMM}); today's register left for testing");
        }

        // ---------------------------------------------------------------- results
        private async Task SeedResultsAsync()
        {
            Console.WriteLine("Assessments, results and report cards");
            var p5 = classes["Primary 5A"];
            var pupils = pupilsByClass["Primary 5A"];

            await As("chinedu.okeke", [Roles.Teacher], async s =>
            {
                var db = Svc<ApplicationDbContext>(s);
                var types = await db.AssessmentTypes.ToDictionaryAsync(t => t.Code, t => t.Id);
                var svc = Svc<IAssessmentService>(s);
                foreach (var code in new[] { "ENG", "MTH", "BSC" })
                {
                    foreach (var (type, title, max) in new[] { ("CA1", "First CA", 20), ("CA2", "Second CA", 20), ("EXAM", "Examination", 60) })
                    {
                        var id = Ok(await svc.CreateAsync(new CreateTermAssessmentRequest
                        {
                            TermId = currentTermId, SchoolClassId = p5, SubjectId = subjects[code], AssessmentTypeId = types[type],
                            Title = title, MaxScore = max, Weight = max, AssessmentDate = termStart.AddDays(type == "CA1" ? 14 : type == "CA2" ? 18 : 21),
                        }), $"assessment {code} {type}");
                        var scores = pupils.Select((pupil, n) => new UpsertAssessmentScoreRequest
                        {
                            TermAssessmentId = id, StudentId = pupil,
                            Score = Math.Round((decimal)(max * (0.45 + (((n * 37 + code.Length * 11 + type.Length * 7) % 50) / 100.0))), 1),
                        }).ToList();
                        Ok(await svc.BulkSetScoresAsync(new BulkSetScoresRequest { TermAssessmentId = id, Scores = scores }), "scores");
                        Ok(await svc.PublishAsync(id), "publish assessment");
                    }
                }
                return 0;
            });

            await As("folake.adeyemi", [Roles.HeadTeacher], async s =>
            {
                var db = Svc<ApplicationDbContext>(s);
                Ok(await Svc<IResultService>(s).ComputeAsync(new ComputeResultsRequest { TermId = currentTermId, SchoolClassId = p5, Finalise = true }), "compute results");
                var generated = Ok(await Svc<IReportCardService>(s).GenerateAsync(new GenerateReportCardsRequest
                { TermId = currentTermId, SchoolClassId = p5, NextTermBegins = termEnd.AddDays(24) }), "generate report cards");

                var cards = await Svc<IReportCardService>(s).ListAsync(new ReportCardFilter { TermId = currentTermId, SchoolClassId = p5 });
                var traits = await db.AffectiveTraits.Select(t => t.Id).ToListAsync();
                var skills = await db.PsychomotorSkills.Select(t => t.Id).ToListAsync();
                var ratings = await db.TraitRatings.OrderBy(r => r.DisplayOrder).Select(r => r.Id).ToListAsync();
                var svc = Svc<IReportCardService>(s);
                // Publish all but the last two, so the head teacher can test publishing.
                foreach (var (card, n) in cards.Select((c, n) => (c, n)))
                {
                    Ok(await svc.UpdateCommentsAsync(new UpdateReportCardCommentsRequest
                    {
                        Id = card.Id,
                        ClassTeacherComment = n % 3 == 0 ? "A diligent pupil who participates well in class." : "Good progress this term; keep practising reading daily.",
                        HeadTeacherComment = "Well done. Keep it up.",
                        NextTermBegins = termEnd.AddDays(24),
                    }), "comments");
                    foreach (var t in traits) Ok(await svc.UpsertAffectiveRatingAsync(new UpsertAffectiveRatingRequest { ReportCardId = card.Id, AffectiveTraitId = t, TraitRatingId = ratings[rng.Next(3)] }), "affective");
                    foreach (var k in skills) Ok(await svc.UpsertPsychomotorRatingAsync(new UpsertPsychomotorRatingRequest { ReportCardId = card.Id, PsychomotorSkillId = k, TraitRatingId = ratings[rng.Next(3)] }), "psychomotor");
                    if (n < cards.Count - 2) Ok(await svc.PublishAsync(card.Id), "publish card");
                }
                return 0;
            });
            Step($"Primary 5A: CA1, CA2 and Examination for English, Mathematics and Basic Science; results computed and finalised; {pupils.Count} report cards ({pupils.Count - 2} published, 2 left as drafts)");
        }

        // ---------------------------------------------------------------- finance
        private async Task SeedFinanceAsync()
        {
            Console.WriteLine("Fees, invoices and payments");
            await As("tunde.bakare", [Roles.SchoolBursar], async s =>
            {
                var db = Svc<ApplicationDbContext>(s);
                var levels = await db.ClassLevels.ToDictionaryAsync(l => l.Name, l => l.Id);
                var cat = await db.FeeCategories.ToDictionaryAsync(c => c.Code, c => c.Id);
                var methods = await db.PaymentMethods.ToDictionaryAsync(m => m.Code, m => m.Id);
                var fees = Svc<IFeeScheduleService>(s);
                var invoices = Svc<IInvoiceService>(s);
                var payments = Svc<IPaymentService>(s);

                foreach (var (level, cls, tuition) in new[] { ("Primary 5", "Primary 5A", 185000m), ("Primary 1", "Primary 1A", 150000m) })
                {
                    var scheduleId = Ok(await fees.CreateAsync(new CreateFeeScheduleRequest
                    { TermId = currentTermId, ClassLevelId = levels[level], Title = $"{level} — First Term fees" }), "fee schedule");
                    var order = 1;
                    foreach (var (code, desc, amount) in new[]
                    {
                        ("TUI", "Tuition", tuition), ("DEV", "Development levy", 25000m), ("EXAM", "Examination fee", 10000m),
                        ("PTA", "PTA levy", 5000m), ("BOOK", "Textbooks and workbooks", 32000m),
                    })
                    {
                        Ok(await fees.UpsertItemAsync(new UpsertFeeScheduleItemRequest
                        { FeeScheduleId = scheduleId, FeeCategoryId = cat[code], Description = desc, Amount = amount, IsMandatory = code != "BOOK", DisplayOrder = order++ }), "fee item");
                    }
                    Ok(await fees.PublishAsync(scheduleId), "publish schedule");
                    Ok(await invoices.IssueAsync(new IssueInvoicesRequest
                    { FeeScheduleId = scheduleId, SchoolClassId = classes[cls], IssuedOn = termStart.AddDays(2), DueDate = termStart.AddDays(30) }), "issue invoices");
                }

                // Payments: most families have paid in full, six have paid half and
                // three nothing yet. Chosen by name (not invoice number, which isn't
                // stable between runs) so the UAT scripts can rely on who owes what.
                string[] unpaid = ["Kamsi Eze", "Oluwaseun Akinola", "Somto Obi"];
                string[] halfPaid = ["Ekaette Okon", "Ebuka Nwachukwu", "Halima Yusuf", "Kehinde Salami", "Opeyemi Adeleke", "Temitope Ogunleye"];
                var all = await invoices.ListAsync(new InvoiceFilter());
                foreach (var (inv, n) in all.OrderBy(i => i.StudentName).Select((inv, n) => (inv, n)))
                {
                    var share = unpaid.Contains(inv.StudentName) ? 0m : halfPaid.Contains(inv.StudentName) ? 0.5m : 1m;
                    if (share == 0) continue;
                    var amount = Math.Round(inv.AmountDue * share, 0);
                    var method = n % 3 == 0 ? "CASH" : n % 3 == 1 ? "BANK" : "POS";
                    Ok(await payments.RecordAsync(new RecordPaymentRequest
                    {
                        StudentId = inv.StudentId, PaymentMethodId = methods[method],
                        PaidOn = termStart.AddDays(3 + n % 10), Amount = amount,
                        Reference = method == "CASH" ? null : $"{method}-{100200 + n}",
                        CollectedById = staff["tunde.bakare"],
                        Allocations = [new AllocationLineRequest { InvoiceId = inv.Id, AmountApplied = amount }],
                    }), $"payment {inv.InvoiceNumber}");
                }
                return 0;
            });
            Step("published First Term fee schedules for Primary 5 and Primary 1; invoices issued to Primary 5A and 1A; 12 paid in full, 6 half-paid, 3 unpaid (Kamsi Eze, Oluwaseun Akinola, Somto Obi)");
        }

        // -------------------------------------------------------------- inventory
        private async Task SeedInventoryAsync()
        {
            Console.WriteLine("Store and inventory");
            await As("musa.ibrahim", [Roles.SchoolStoreKeeper], async s =>
            {
                var db = Svc<ApplicationDbContext>(s);
                var cat = await db.ItemCategories.ToDictionaryAsync(c => c.Code, c => c.Id);
                var uom = await db.UnitsOfMeasure.ToDictionaryAsync(u => u.Code, u => u.Id);
                var types = await db.StockMovementTypes.ToDictionaryAsync(t => t.Code, t => t.Id);
                var supplierIds = new List<Guid>();
                foreach (var (name, contact, phone) in new[]
                {
                    ("Lagos Book Hub Ltd", "Mr Femi Ade", "08023330001"),
                    ("Uniforms & More Enterprises", "Mrs Joy Okon", "08023330002"),
                    ("CleanPro Supplies", "Mr Sule Garba", "08023330003"),
                })
                {
                    supplierIds.Add(Ok(await Svc<ISupplierService>(s).CreateAsync(new CreateSupplierRequest
                    { Name = name, ContactName = contact, Phone = phone, Email = $"sales@{name.Split(' ')[0].ToLowerInvariant()}.ng", Address = "Lagos" }), $"supplier {name}"));
                }

                var items = new Dictionary<string, Guid>();
                foreach (var (name, sku, c, u, reorder, qty, cost) in new[]
                {
                    ("Primary 5 English textbook", "BK-ENG-P5", "BOOK", "EA", 10m, 40m, 3500m),
                    ("Primary 5 Mathematics textbook", "BK-MTH-P5", "BOOK", "EA", 10m, 35m, 3800m),
                    ("Exercise books (40 leaves)", "ST-EXB-40", "STAT", "PK", 20m, 60m, 1200m),
                    ("HB pencils", "ST-PEN-HB", "STAT", "BOX", 5m, 12m, 900m),
                    ("School sweater (size 8)", "UN-SWT-08", "UNIF", "PC", 15m, 30m, 4500m),
                    ("School shirt (size 8)", "UN-SHT-08", "UNIF", "PC", 15m, 45m, 3000m),
                    ("Football", "SP-FTB", "SPRT", "EA", 3m, 6m, 7500m),
                    ("Liquid soap (5L)", "CL-SOAP-5", "CLN", "L", 4m, 5m, 2800m),
                    ("First-aid kit", "MD-FAK", "MED", "SET", 2m, 3m, 12000m),
                    ("Whiteboard markers", "ST-WBM", "STAT", "BOX", 6m, 4m, 2500m),
                })
                {
                    items[sku] = Ok(await Svc<IStoreItemService>(s).CreateAsync(new CreateStoreItemRequest
                    { Name = name, Sku = sku, ItemCategoryId = cat[c], UnitOfMeasureId = uom[u], ReorderLevel = reorder, OpeningQuantity = qty, OpeningUnitCost = cost }), $"item {sku}");
                }

                var mv = Svc<IStockMovementService>(s);
                async Task Move(string sku, string type, decimal qty, decimal cost, int daysAgo, Guid? supplier = null, Guid? student = null, Guid? cls = null, string? note = null) =>
                    Ok(await mv.RecordAsync(new RecordStockMovementRequest
                    {
                        StoreItemId = items[sku], StockMovementTypeId = types[type], MovedOn = today.AddDays(-daysAgo),
                        Quantity = qty, UnitCost = cost, ReceivedFromSupplierId = supplier, IssuedToStudentId = student,
                        IssuedToSchoolClassId = cls, PerformedById = staff["musa.ibrahim"], Notes = note,
                        Reference = type == "PURCHASE" ? $"LPO-{2600 + daysAgo}" : null,
                    }), $"movement {sku} {type}");

                await Move("BK-ENG-P5", "PURCHASE", 20, 3500, 12, supplier: supplierIds[0]);
                await Move("BK-MTH-P5", "PURCHASE", 20, 3800, 12, supplier: supplierIds[0]);
                await Move("UN-SHT-08", "PURCHASE", 25, 3000, 10, supplier: supplierIds[1]);
                await Move("BK-ENG-P5", "ISSUE", 1, 3500, 8, student: pupilsByClass["Primary 5A"][0]);
                await Move("BK-MTH-P5", "ISSUE", 1, 3800, 8, student: pupilsByClass["Primary 5A"][0]);
                await Move("ST-EXB-40", "ISSUE", 12, 1200, 7, cls: classes["Primary 5A"]);
                await Move("ST-PEN-HB", "ISSUE", 9, 900, 6, cls: classes["Primary 1A"], note: "Pencils for Primary 1A");
                await Move("CL-SOAP-5", "PURCHASE", 10, 2800, 5, supplier: supplierIds[2]);
                await Move("UN-SWT-08", "WRITEOFF", 2, 4500, 3, note: "Damaged in storage");
                return 0;
            });
            Step("3 suppliers, 10 store items with opening balances, purchases / issues / a write-off; pencils and markers left below reorder level");
        }

        // --------------------------------------------------------- communications
        private async Task SeedCommunicationsAsync()
        {
            Console.WriteLine("Announcements and reminder preferences");
            await As("folake.adeyemi", [Roles.HeadTeacher], async s =>
            {
                var db = Svc<ApplicationDbContext>(s);
                var cat = await db.AnnouncementCategories.ToDictionaryAsync(c => c.Code, c => c.Id);
                var aud = await db.AnnouncementAudiences.ToDictionaryAsync(a => a.Code, a => a.Id);
                var svc = Svc<IAnnouncementService>(s);
                async Task Post(string title, string body, string category, string audience, bool publish, bool pinned = false, Guid? cls = null, DateOnly? expires = null) =>
                    Ok(await svc.CreateAsync(new CreateAnnouncementRequest
                    {
                        Title = title, Body = body, CategoryId = cat[category], AudienceId = aud[audience], TargetSchoolClassId = cls,
                        PublishImmediately = publish, IsPinned = pinned, ExpiresOn = expires,
                    }), $"announcement {title}");

                await Post("Welcome back to a new term!", "Dear parents and pupils,\n\nWelcome back. Classes run from 8:00am to 2:00pm. Please ensure pupils arrive by 7:45am in full school uniform.\n\nThank you.",
                    "GEN", "ALL", publish: true, pinned: true);
                await Post("PTA meeting this Saturday", "The first PTA meeting of the term holds this Saturday at 10:00am in the school hall. All parents are encouraged to attend.",
                    "EVENT", "PARENT", publish: true, expires: today.AddDays(14));
                await Post("Primary 5A excursion to the National Museum", "Primary 5A will visit the National Museum, Onikan, on Friday. Please return the signed consent form by Wednesday.",
                    "ACAD", "CLASS", publish: true, cls: classes["Primary 5A"]);
                await Post("Mid-term break dates", "Mid-term break dates will be confirmed after the PTA meeting.", "HOL", "ALL", publish: false);
                return 0;
            });

            // A couple of families have set reminder preferences.
            await As("folake.adeyemi", [Roles.HeadTeacher], async s =>
            {
                var prefs = Svc<INotificationPreferenceService>(s);
                Ok(await prefs.SaveForUserAsync(parents[1].UserId, new UpdateNotificationPreferenceRequest { EmailEnabled = true, SmsEnabled = false }), "prefs");
                Ok(await prefs.SaveForUserAsync(parents[2].UserId, new UpdateNotificationPreferenceRequest
                { EmailEnabled = true, SmsEnabled = true, QuietHoursEnabled = true, QuietHoursStart = new(21, 0), QuietHoursEnd = new(7, 0) }), "prefs");
                return 0;
            });
            Step("4 announcements (pinned welcome, PTA meeting for parents, Primary 5A class notice, 1 draft); reminders queued automatically");
            Step($"reminder preferences: {parents[1].Name} (SMS off), {parents[2].Name} (quiet hours 21:00–07:00)");
        }

        // -------------------------------------------------------------- messaging
        private async Task SeedMessagingAsync()
        {
            Console.WriteLine("Messages");
            var (_, parentUser, parentName) = parents[0];
            var ward = parentOfPupil.First(kv => kv.Value == parents[0].ParentId).Key;
            Guid threadId = Guid.Empty;
            actor.UserId = parentUser; actor.UserName = parentName; actor.Roles = [Roles.Parent];
            await using (var s = root.CreateAsyncScope())
            {
                threadId = Ok(await Svc<IMessagingService>(s).StartThreadAsync(new StartThreadRequest
                {
                    StudentId = ward, Subject = "Replacement reading book",
                    Body = "Good morning,\nMy daughter misplaced her reading book. How can we get a replacement?\n\nThank you.",
                }), "parent thread");
            }
            await As("folake.adeyemi", [Roles.HeadTeacher], async s =>
            {
                Ok(await Svc<IMessagingService>(s).ReplyAsync(threadId, "Good morning. Please send ₦3,500 with her tomorrow and the storekeeper will issue a new copy."), "reply");
                Ok(await Svc<IMessagingService>(s).StartThreadAsync(new StartThreadRequest
                {
                    FamilyUserId = parents[4].UserId, Subject = "Outstanding school fees",
                    Body = "Dear parent, this is a gentle reminder that part of this term's fees is still outstanding. Kindly visit the bursary or pay online.",
                }), "office thread");
                return 0;
            });
            actor.UserId = parents[3].UserId; actor.UserName = parents[3].Name; actor.Roles = [Roles.Parent];
            await using (var s = root.CreateAsyncScope())
            {
                Ok(await Svc<IMessagingService>(s).StartThreadAsync(new StartThreadRequest
                { Subject = "School bus route", Body = "Does the school bus pass through Ogui Road? We just moved." }), "unanswered thread");
            }
            Step($"3 conversations: {parentName} ↔ office (answered), office → {parents[4].Name} (fees reminder), {parents[3].Name} (awaiting reply)");
        }
    }
}
