using MemoRecipeIA.Application.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Net;
using System.Text.Json;

namespace MemoRecipeIA.Functions
{
    public class ExtractOcrFunction
    {
        private readonly ILogger _logger;
        private readonly IRecipePipeline _pipeline;

        public ExtractOcrFunction(
            ILoggerFactory loggerFactory,
            IRecipePipeline pipeline)
        {
            _logger = loggerFactory.CreateLogger<ExtractOcrFunction>();
            _pipeline = pipeline;
        }

        [Function("ExtractOcrFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
            HttpRequestData req)
        {
            _logger.LogInformation("Recipe import request received.");

            // Check content type
            if (!req.Headers.TryGetValues("Content-Type", out var contentTypes))
                return await Error(req, "Missing Content-Type header.");

            var contentType = contentTypes.First();
            if (!contentType.Contains("multipart/form-data"))
                return await Error(req, "Content-Type must be multipart/form-data.");

            // Extract boundary
            var boundary = HeaderUtilities
                .RemoveQuotes(contentType.Split("boundary=").Last())
                .Value;

            if (boundary == null)
                return await Error(req, "Missing boundary.");

            var reader = new MultipartReader(boundary, req.Body);
            var section = await reader.ReadNextSectionAsync();

            if (section == null)
                return await Error(req, "No file found.");

            // Check image
            if (section.Headers.TryGetValue("Content-Type", out var fileType)
                && !fileType.ToString().StartsWith("image/"))
            {
                return await Error(req, "File must be an image.");
            }

            // Read image
            using var imageStream = new MemoryStream();
            await section.Body.CopyToAsync(imageStream);
            imageStream.Position = 0;

            // Pipeline finish
            var recipe = await _pipeline.ProcessAsync(imageStream);

            // Response
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            await response.WriteStringAsync(
                JsonSerializer.Serialize(recipe, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));

            return response;
        }

        private static async Task<HttpResponseData> Error(
            HttpRequestData req, string message)
        {
            var res = req.CreateResponse(HttpStatusCode.BadRequest);
            await res.WriteStringAsync(JsonSerializer.Serialize(new
            {
                success = false,
                error = message
            }));
            return res;
        }
    }
}
