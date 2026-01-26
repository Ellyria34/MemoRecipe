namespace memorecipe_ia.Infrastructure.OCR;

public interface IOcrService
{
    /// <summary>
    /// Extract text from an image stream using OCR.
    /// </summary>
    /// <param name="imageStream">Image stream</param>
    /// <returns>Extracted text as string</returns>
    Task<string> ExtractAsync(Stream imageStream);
}

