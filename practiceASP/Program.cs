using DIE.Endpoints;
using practiceASP;
using System.Text;


var builder = WebApplication.CreateBuilder(args);
var ocrApiKey = builder.Configuration["OCRSpace:ApiKey"];

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

Directory.CreateDirectory("Docs");

DocumentEndpoints.Map(app);
DocumentInfoEndpoints.Map(
    app,
    builder.Configuration["OCRSpace:ApiKey"]
);

app.Run();
