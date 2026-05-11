using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Family;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class StudentPhotoService(
    ApplicationDbContext db,
    IWebHostEnvironment env) : IStudentPhotoService
{
    // Photos live under wwwroot/uploads/students. The folder is created on
    // demand so a fresh checkout does not have to ship empty directories.
    private const string PhotoFolderRelative = "uploads/students";

    // 5 MB cap is plenty for a profile picture and protects against accidents.
    public const long MaxPhotoBytes = 5 * 1024 * 1024;

    public static readonly IReadOnlyDictionary<string, string> AllowedTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/jpg"]  = ".jpg",
            ["image/png"]  = ".png",
            ["image/webp"] = ".webp",
        };

    public async Task<OperationResult<string>> UploadAsync(
        Guid studentId,
        Stream content,
        string contentType,
        long length,
        CancellationToken ct = default)
    {
        if (length <= 0)
            return OperationResult<string>.Failure("Selected file is empty.");

        if (length > MaxPhotoBytes)
            return OperationResult<string>.Failure(
                $"Photo is too large. Maximum size is {MaxPhotoBytes / (1024 * 1024)} MB.");

        if (string.IsNullOrWhiteSpace(contentType)
            || !AllowedTypes.TryGetValue(contentType, out var extension))
            return OperationResult<string>.Failure(
                "Unsupported image format. Use JPG, PNG, or WebP.");

        var student = await db.Students.FirstOrDefaultAsync(s => s.Id == studentId, ct);
        if (student is null)
            return OperationResult<string>.Failure("Student not found.");

        var webRoot = env.WebRootPath
            ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, PhotoFolderRelative);
        Directory.CreateDirectory(folder);

        // Stable file name keyed by Student.Id keeps the schema simple and
        // means re-uploads naturally overwrite the previous photo on disk.
        // Removing every other extension for this pupil avoids leaving an
        // orphan file behind when the format changes (e.g. jpg -> png).
        foreach (var existing in Directory.EnumerateFiles(folder, $"{studentId}.*"))
        {
            try { File.Delete(existing); } catch { /* best-effort cleanup */ }
        }

        var fileName = $"{studentId}{extension}";
        var path = Path.Combine(folder, fileName);
        await using (var fs = File.Create(path))
        {
            content.Position = content.CanSeek ? 0 : content.Position;
            await content.CopyToAsync(fs, ct);
        }

        // PhotoUrl is the public-facing URL the browser will fetch. The
        // app.MapStaticAssets() pipeline serves the file directly from
        // wwwroot, so a relative URL is enough.
        var publicUrl = "/" + PhotoFolderRelative + "/" + fileName;
        student.PhotoUrl = publicUrl;
        await db.SaveChangesAsync(ct);

        return OperationResult<string>.Success(publicUrl);
    }

    public async Task<OperationResult> RemoveAsync(Guid studentId, CancellationToken ct = default)
    {
        var student = await db.Students.FirstOrDefaultAsync(s => s.Id == studentId, ct);
        if (student is null) return OperationResult.Failure("Student not found.");

        var webRoot = env.WebRootPath
            ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, PhotoFolderRelative);
        if (Directory.Exists(folder))
        {
            foreach (var existing in Directory.EnumerateFiles(folder, $"{studentId}.*"))
            {
                try { File.Delete(existing); } catch { /* best-effort */ }
            }
        }

        student.PhotoUrl = null;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }
}
