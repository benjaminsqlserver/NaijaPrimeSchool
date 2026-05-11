using NaijaPrimeSchool.Application.Common;

namespace NaijaPrimeSchool.Application.Family;

public interface IStudentPhotoService
{
    Task<OperationResult<string>> UploadAsync(
        Guid studentId,
        Stream content,
        string contentType,
        long length,
        CancellationToken ct = default);

    Task<OperationResult> RemoveAsync(Guid studentId, CancellationToken ct = default);
}
