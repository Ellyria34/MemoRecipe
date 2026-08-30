using Microsoft.Extensions.Logging;

namespace MemoRecipe.Application.Services.Upload;

public class FileUploadValidator : IFileUploadValidator
{
    // Values mirror the previous inline validation in RecipeController (behavior-preserving refactor)
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };
    private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/png" };
    private const int MagicBytesLength = 8;

    // Image signatures (magic bytes)
    private static readonly byte[] JpegSignature = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    private readonly ILogger<FileUploadValidator> _logger;

    public FileUploadValidator(ILogger<FileUploadValidator> logger)
    {
        _logger = logger;
    }

    public async Task<FileUploadValidationResult> ValidateAsync(
        Stream stream,
        string fileName,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        // Gate 1 - Extension (cheapest check, no stream read)
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return FileUploadValidationResult.ExtensionNotAllowed;
        }

        // Gate 2 - MIME type (declared by client, still cheap)
        if (!AllowedMimeTypes.Contains(mimeType))
        {
            return FileUploadValidationResult.MimeTypeNotAllowed;
        }

        // Gate 3 - Magic bytes (defensive read: ReadAsync + bytesRead check
        // instead of ReadExactlyAsync which throws EndOfStreamException on short files - US-03 fix)
        var magicBytes = new byte[MagicBytesLength];
        var bytesRead = await stream.ReadAsync(magicBytes.AsMemory(0, MagicBytesLength), cancellationToken);

        if (bytesRead < MagicBytesLength)
        {
            // RGPD Art. 5 minimization: log only the size, never the file content
            _logger.LogWarning("UploadTooSmallReceived - Size {Size} bytes (min {Min})", bytesRead, MagicBytesLength);
            return FileUploadValidationResult.FileTooSmall;
        }

        if (!IsValidImageMagicBytes(magicBytes))
        {
            return FileUploadValidationResult.InvalidMagicBytes;
        }

        // Reset stream position for downstream consumers (OCR, LLM). Guarded: not all streams are seekable.
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        return FileUploadValidationResult.Valid;
    }

    private static bool IsValidImageMagicBytes(byte[] magicBytes)
    {
        // JPEG: FF D8 FF (first 3 bytes)
        if (magicBytes.Take(3).SequenceEqual(JpegSignature))
        {
            return true;
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A (8 bytes signature)
        if (magicBytes.Take(8).SequenceEqual(PngSignature))
        {
            return true;
        }

        return false;
    }
}
