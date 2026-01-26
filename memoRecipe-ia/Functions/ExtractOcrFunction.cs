using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using System.Text.Json;
using memorecipe_ia.Infrastructure.OCR;
using Microsoft.Net.Http.Headers;

namespace MemoRecipeIA.Functions
{
    public class ExtractOcrFunction
    {
        private readonly ILogger _logger;
        private readonly IOcrService _ocr;

        public ExtractOcrFunction(ILoggerFactory loggerFactory, IOcrService ocr)
        {
            _logger = loggerFactory.CreateLogger<ExtractOcrFunction>();
            _ocr = ocr;
        }

        [Function("ExtractOcrFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("OCR request received.");

            // 1️⃣ Vérifier Content-Type / multipart
            if (!req.Headers.TryGetValues("Content-Type", out var contentTypes))
            {
                return await CreateError(req, "Missing Content-Type header.");
            }

            var contentType = contentTypes.First();
            if (!contentType.Contains("multipart/form-data"))
            {
                return await CreateError(req, "Content-Type must be multipart/form-data.");
            }

            // 2️⃣ Extraire le boundary
            var boundary = HeaderUtilities.RemoveQuotes(
                contentType.Split("boundary=").Last()
            ).Value;

            if (boundary == null)
            {
                return await CreateError(req, "Missing boundary in Content-Type.");
            }

            var reader = new MultipartReader(boundary, req.Body);
            var section = await reader.ReadNextSectionAsync();

            if (section == null)
            {
                return await CreateError(req, "No file section found in form-data.");
            }

            // 3️⃣ Vérifier que c’est un fichier image (optionnel mais recommandé)
            if (section.Headers.ContainsKey("Content-Type"))
            {
                var fileContentType = section.Headers["Content-Type"].ToString();

                if (!fileContentType.StartsWith("image/"))
                {
                    return await CreateError(req, "Uploaded file must be an image.");
                }
            }


            // 4️⃣ Lire l’image
            using var imageStream = new MemoryStream();
            await section.Body.CopyToAsync(imageStream);
            imageStream.Position = 0;

            // 5️⃣ Appeler OCR
            string extractedText;
            try
            {
                extractedText = await _ocr.ExtractAsync(imageStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR error");
                return await CreateError(req, "OCR engine failed: " + ex.Message);
            }

            // 6️⃣ Retourner la réponse JSON
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            response.WriteString(JsonSerializer.Serialize(new
            {
                success = true,
                text = extractedText
            }));

            return response;
        }

        private async Task<HttpResponseData> CreateError(HttpRequestData req, string msg)
        {
            var error = req.CreateResponse(HttpStatusCode.BadRequest);
            await error.WriteStringAsync(JsonSerializer.Serialize(new
            {
                success = false,
                error = msg
            }));
            return error;
        }
    }
}
