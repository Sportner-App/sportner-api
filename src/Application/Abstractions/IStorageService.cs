namespace Sportner.Application.Abstractions;

public interface IStorageService
{
    Task<string> UploadAvatarAsync(
        Guid userId,
        Stream fileStream,
        string contentType,
        string fileExtension,
        CancellationToken cancellationToken = default);
}
