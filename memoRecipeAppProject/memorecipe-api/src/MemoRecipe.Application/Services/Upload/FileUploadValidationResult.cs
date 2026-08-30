namespace MemoRecipe.Application.Services.Upload;

public enum FileUploadValidationResult
{
    Valid,
    ExtensionNotAllowed,
    MimeTypeNotAllowed,
    FileTooSmall,
    InvalidMagicBytes
}
