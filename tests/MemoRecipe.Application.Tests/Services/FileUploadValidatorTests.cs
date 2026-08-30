using MemoRecipe.Application.Services.Upload;
using Microsoft.Extensions.Logging.Abstractions;

namespace MemoRecipe.Application.Tests.Services;

public class FileUploadValidatorTests
{
    private readonly FileUploadValidator _validator;

    // JPEG magic bytes (FF D8 FF) + 5 padding bytes to reach the 8-byte magic buffer
    private static readonly byte[] ValidJpegBytes = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x00, 0x00, 0x00 };

    // PNG signature (89 50 4E 47 0D 0A 1A 0A) - exactly 8 bytes
    private static readonly byte[] ValidPngBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    public FileUploadValidatorTests()
    {
        _validator = new FileUploadValidator(NullLogger<FileUploadValidator>.Instance);
    }

    [Fact]
    public async Task ValidateAsync_WithValidJpeg_ReturnsValid()
    {
        // Arrange
        using var stream = new MemoryStream(ValidJpegBytes);

        // Act
        var result = await _validator.ValidateAsync(stream, "photo.jpg", "image/jpeg");

        // Assert
        Assert.Equal(FileUploadValidationResult.Valid, result);
    }

    [Fact]
    public async Task ValidateAsync_WithValidPng_ReturnsValid()
    {
        // Arrange
        using var stream = new MemoryStream(ValidPngBytes);

        // Act
        var result = await _validator.ValidateAsync(stream, "photo.png", "image/png");

        // Assert
        Assert.Equal(FileUploadValidationResult.Valid, result);
    }

    [Fact]
    public async Task ValidateAsync_WithDisallowedExtension_ReturnsExtensionNotAllowed()
    {
        // Arrange
        using var stream = new MemoryStream(ValidJpegBytes);

        // Act - .exe not in AllowedExtensions
        var result = await _validator.ValidateAsync(stream, "malware.exe", "image/jpeg");

        // Assert
        Assert.Equal(FileUploadValidationResult.ExtensionNotAllowed, result);
    }

    [Fact]
    public async Task ValidateAsync_WithDisallowedMimeType_ReturnsMimeTypeNotAllowed()
    {
        // Arrange
        using var stream = new MemoryStream(ValidJpegBytes);

        // Act - text/plain not in AllowedMimeTypes
        var result = await _validator.ValidateAsync(stream, "photo.jpg", "text/plain");

        // Assert
        Assert.Equal(FileUploadValidationResult.MimeTypeNotAllowed, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(7)]
    public async Task ValidateAsync_WithFileSmallerThan8Bytes_ReturnsFileTooSmall(int size)
    {
        // Arrange - the US-03 fix: files < 8 bytes must NOT throw EndOfStreamException
        var bytes = new byte[size];
        for (int i = 0; i < size; i++) bytes[i] = 0xFF; // arbitrary content, won't matter (rejected on size)
        using var stream = new MemoryStream(bytes);

        // Act
        var result = await _validator.ValidateAsync(stream, "photo.jpg", "image/jpeg");

        // Assert
        Assert.Equal(FileUploadValidationResult.FileTooSmall, result);
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidMagicBytes_ReturnsInvalidMagicBytes()
    {
        // Arrange - 8 bytes that match no known image signature (GIF signature "GIF87a" for example)
        var bytes = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61, 0x00, 0x00 };
        using var stream = new MemoryStream(bytes);

        // Act - extension .jpg + mime image/jpeg pass gates 1-2, but magic bytes fail gate 3
        var result = await _validator.ValidateAsync(stream, "photo.jpg", "image/jpeg");

        // Assert
        Assert.Equal(FileUploadValidationResult.InvalidMagicBytes, result);
    }

    [Fact]
    public async Task ValidateAsync_WithValidFile_ResetsStreamPositionForDownstream()
    {
        // Arrange - critical contract: after successful validation, the stream must be readable from position 0
        // (OCR / LLM downstream consumers rely on this)
        using var stream = new MemoryStream(ValidJpegBytes);

        // Act
        var result = await _validator.ValidateAsync(stream, "photo.jpg", "image/jpeg");

        // Assert
        Assert.Equal(FileUploadValidationResult.Valid, result);
        Assert.Equal(0, stream.Position);
    }
}
