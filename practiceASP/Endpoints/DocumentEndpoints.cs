namespace DIE.Endpoints
{
    public static class DocumentEndpoints
    {
        public static void Map(WebApplication app)
        {
            //an api Post that takes the uploaded pdf and saves it in Docs folder
            app.MapPost("/api/docs", (IFormFile file) =>
            {
                if (file.Length > 52428800)
                {
                    return Results.BadRequest("File size exceeds the limit of 50 MB.");
                }

                if (file.Length == 0)
                {
                    return Results.BadRequest("File is empty.");
                }

                if (Path.GetExtension(file.FileName).ToLower() != ".pdf")
                {
                    return Results.BadRequest("Only PDF files are allowed.");
                }

                var fileName = Path.GetFileName(file.FileName);

                int fileCount = 1;

                while (true)
                {
                    var newFileName = $"{fileCount}_{fileName}";
                    var filePath = Path.Combine("Docs", newFileName);

                    try
                    {
                        using var stream = new FileStream(
                            filePath,
                            FileMode.CreateNew
                        );

                        file.CopyTo(stream);

                        return Results.Ok(newFileName);
                    }
                    catch (IOException)
                    {
                        fileCount++;
                    }
                }
            })
            .DisableAntiforgery();

            //an api Get that returns the list of all pdf files in Docs folder
            app.MapGet("/api/docs", () =>
            {
                var files = Directory.GetFiles("Docs", "*.pdf");
                var fileNames = files.Select(f => Path.GetFileName(f)).ToList();
                return Results.Ok(fileNames);
            });

            //an api that gives you and specific pdf file based on name
            app.MapGet("/api/docs/{id}", (string id) =>
            {
                var filePath = Path.Combine("Docs", id);
                if (!System.IO.File.Exists(filePath))
                {
                    return Results.NotFound();
                }
                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                return Results.File(fileBytes, "application/pdf", id);
            });
        }
    }
}
