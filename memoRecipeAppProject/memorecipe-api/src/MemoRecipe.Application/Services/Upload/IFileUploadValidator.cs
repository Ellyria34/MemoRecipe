namespace MemoRecipe.Application.Services.Upload;

public interface IFileUploadValidator
{
    Task<FileUploadValidationResult> ValidateAsync(
        Stream stream,
        string fileName,
        string mimeType,
        CancellationToken cancellationToken = default);
}