using MemoRecipeIA.Application.Interfaces;
using Tesseract;

namespace MemoRecipeIA.Infrastructure.OCR
{
    public class TesseractOcrService : IOcrService
    {
        public async Task<string> ExtractAsync(Stream imageStream)
        {
            using var ms = new MemoryStream();
            await imageStream.CopyToAsync(ms);
            var imageBytes = ms.ToArray();

            using var pix = Pix.LoadFromMemory(imageBytes);

            using var engine = new TesseractEngine(
                @"C:\Program Files\Tesseract-OCR\tessdata",
                "fra+eng",
                EngineMode.Default
            );

            using var page = engine.Process(pix);
            return page.GetText();
        }
    }
}

