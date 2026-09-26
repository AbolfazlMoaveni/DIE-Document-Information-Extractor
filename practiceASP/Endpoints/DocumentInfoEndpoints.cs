using Microsoft.AspNetCore.Mvc.RazorPages;
using practiceASP;
using SkiaSharp;
using System.IO;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Rendering.Skia;
using static System.Net.Mime.MediaTypeNames;

namespace DIE.Endpoints
{
    public static class DocumentInfoEndpoints
    {
        public static void Map(WebApplication app, string? ocrApiKey)
        {

            //api to give preview and little info
            app.MapGet("/api/docs/{id}/info", (string id) =>
            {
                var filePath = Path.Combine("Docs", id);

                if (!System.IO.File.Exists(filePath))
                {
                    return Results.NotFound();
                }

                string name = id;
                int pagenum;
                string preview;
                string title = "";
                string author = "";
                using (PdfDocument document = PdfDocument.Open(filePath))
                {
                    author = document.Information.Author;
                    title = document.Information.Title;
                    pagenum = document.NumberOfPages;
                    if(document.GetPage(1).Text.Length < 200)
                    {
                        preview = ContentOrderTextExtractor.GetText(document.GetPage(1));
                    }
                    else
                    {
                        preview = ContentOrderTextExtractor.GetText(document.GetPage(1)).Remove(199) + "...";
                    }
                }
                return Results.Ok(new
                {
                    Doc_name = name,
                    number_of_pages = pagenum,
                    title = title,
                    author = author,
                    preview = preview
                });
            });

            // API that gives information about the PDF
            app.MapGet("/api/docs/{id}/fullinfo", async (string id) =>
            {
                var filePath = Path.Combine("Docs", id);

                if (!System.IO.File.Exists(filePath))
                {
                    return Results.NotFound();
                }

                string name = id;

                List<int> textBasedPages = new List<int>();
                List<int> picBasedPages = new List<int>();

                int numPicPages = 0;
                int numTextPages = 0;

                using (PdfDocument document = PdfDocument.Open(filePath))
                {
                    int nump = document.NumberOfPages;

                    StringBuilder allText = new StringBuilder();

                    foreach (UglyToad.PdfPig.Content.Page page in document.GetPages())
                    {
                        string text = ContentOrderTextExtractor.GetText(page);

                        if (text.Trim().Length > 0)
                        {
                            // This page already contains extractable text
                            textBasedPages.Add(page.Number);
                            numTextPages++;

                            allText.AppendLine(text);
                        }
                        else
                        {
                            // This page is image-based, so use OCR
                            picBasedPages.Add(page.Number);
                            numPicPages++;

                            string ocrText = await Extract_text(
                                filePath,
                                page.Number,
                                ocrApiKey
                            );

                            allText.AppendLine(ocrText);
                        }
                    }

                    DocInfo info = new DocInfo(
                        name,
                        nump,
                        allText.ToString(),
                        numPicPages,
                        numTextPages,
                        picBasedPages,
                        textBasedPages
                    );

                    return Results.Ok(info);
                }
            });
        }


        private static async Task<string> Extract_text(
            string docPath,
            int pageNum,
            string? apiKey)
        {
            // Folder for temporary rendered images
            string imageDirectory = Path.Combine("Docs", "tempimage");

            Directory.CreateDirectory(imageDirectory);

            // Example:
            // Docs/tempimage/1_paper_3.png
            string imagePath = Path.Combine(
                imageDirectory,
                $"{Path.GetFileNameWithoutExtension(docPath)}_{pageNum}.png"
            );


            // Open PDF
            using (PdfDocument document = PdfDocument.Open(docPath))
            {
                // Enable Skia rendering
                document.AddSkiaPageFactory();

                float scale = 2.0f;

                // Render PDF page as an image
                using (SKBitmap skBitmap =
                       document.GetPageAsSKBitmap(
                           pageNum,
                           scale,
                           SKColors.White))
                {
                    // Convert bitmap to PNG
                    using (var image = SKImage.FromBitmap(skBitmap))
                    using (var data = image.Encode(
                        SKEncodedImageFormat.Png,
                        100))
                    using (var stream = File.OpenWrite(imagePath))
                    {
                        data.SaveTo(stream);
                    }
                }
            }

            Console.WriteLine(
                $"OCR image: {imagePath}, " +
                $"size: {new FileInfo(imagePath).Length / 1024} KB"
            );
            // Send image to OCR.space
            return await OCR_imgRead(imagePath, apiKey);
        }


        private static readonly HttpClient client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(2)
        };


        private static async Task<string> OCR_imgRead(
            string imgPath,
            string? apiKey)
        {
            // Make sure an API key actually exists
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception(
                    "OCR.space API key is missing. " +
                    "Make sure it is configured correctly."
                );
            }


            if (!File.Exists(imgPath))
            {
                throw new FileNotFoundException(
                    "The specified image file was not found.",
                    imgPath
                );
            }


            // Official OCR.space API endpoint
            string url = "https://api.ocr.space/parse/image";


            try
            {
                using (var multipartFormContent =
                       new MultipartFormDataContent())
                {
                    // API key
                    multipartFormContent.Add(
                        new StringContent(apiKey),
                        "apikey"
                    );

                    // Language
                    multipartFormContent.Add(
                        new StringContent("eng"),
                        "language"
                    );

                    // OCR Engine
                    //multipartFormContent.Add(
                    //    new StringContent("3"),
                    //    "OCREngine"
                    //);


                    // Read image file
                    byte[] fileBytes =
                        await File.ReadAllBytesAsync(imgPath);

                    var fileContent =
                        new ByteArrayContent(fileBytes);

                    string fileName =
                        Path.GetFileName(imgPath);


                    multipartFormContent.Add(
                        fileContent,
                        "file",
                        fileName
                    );


                    // Send request
                    HttpResponseMessage response =
                        await client.PostAsync(
                            url,
                            multipartFormContent
                        );


                    response.EnsureSuccessStatusCode();


                    // Read JSON response
                    string jsonResponse =
                        await response.Content.ReadAsStringAsync();


                    // Parse JSON
                    using (JsonDocument doc =
                           JsonDocument.Parse(jsonResponse))
                    {
                        JsonElement root =
                            doc.RootElement;


                        // Check OCR.space errors
                        if (root.TryGetProperty(
                                "IsErroredOnProcessing",
                                out var errored) &&
                            errored.ValueKind ==
                            JsonValueKind.True)
                        {
                            string errMsg =
                                "OCR.space reported an error.";


                            if (root.TryGetProperty(
                                    "ErrorMessage",
                                    out var errElem))
                            {
                                if (errElem.ValueKind ==
                                    JsonValueKind.Array)
                                {
                                    errMsg =
                                        string.Join(
                                            " ",
                                            errElem
                                                .EnumerateArray()
                                                .Select(x => x.ToString())
                                        );
                                }
                                else if (errElem.ValueKind ==
                                         JsonValueKind.String)
                                {
                                    errMsg =
                                        errElem.GetString()
                                        ?? errMsg;
                                }
                            }


                            throw new Exception(errMsg);
                        }


                        // Extract ParsedResults[0].ParsedText
                        if (root.TryGetProperty(
                                "ParsedResults",
                                out var parsedResults) &&
                            parsedResults.ValueKind ==
                            JsonValueKind.Array &&
                            parsedResults.GetArrayLength() > 0)
                        {
                            var first =
                                parsedResults[0];


                            if (first.TryGetProperty(
                                    "ParsedText",
                                    out var parsedText))
                            {
                                return parsedText.GetString()
                                       ?? string.Empty;
                            }
                        }


                        return string.Empty;
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception(
                    "OCR.space request timed out. The OCR server did not respond within the configured timeout.",
                    ex
                );
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"OCR_imgRead failed: {ex.Message}",
                    ex
                );
            }
        }
    }
}