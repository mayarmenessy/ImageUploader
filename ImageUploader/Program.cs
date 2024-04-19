using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAntiforgery();
var app = builder.Build();

app.UseAntiforgery();

app.MapGet("/", (HttpContext context, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context);
    return Results.Content(
        $"""
         <!DOCTYPE html>
         <html lang="en">
         <head>
             <meta charset="UTF-8">
             <meta name="viewport" content="width=device-width, initial-scale=1.0">
             <title>Upload Image</title>
             <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-QWTKZyjpPEjISv5WaRU9OFeRpok6YctnYmDr5pNlyT2bRjXh0JMhjY6hW+ALEwIH" crossorigin="anonymous">
         </head>
         <body>
             <div class="container">
                 <h1>Upload Image</h1>
                 <form action="/upload" method="post" enctype="multipart/form-data">
                     <!-- Antiforgery token field -->
                     <input name="{token.FormFieldName}" type="hidden" value="{token.RequestToken}" />

                     <!-- Title input field -->
                     <div class="mb-3">
                         <label for="title" class="form-label">Title</label>
                         <input type="text" class="form-control" id="title" name="title" required>
                     </div>

                     <!-- File input field -->
                     <div class="mb-3">
                         <label for="file" class="form-label">Choose File</label>
                         <input type="file" class="form-control" id="file" name="file" accept=".jpeg, .png, .gif" required>
                     </div>

                     <!-- Upload button -->
                     <button type="submit" class="btn btn-primary">Upload</button>
                 </form>
             </div>
         </body>
         </html>
         
         
         """, "text/html");
});


app.MapPost("/upload", async (IFormFile file, [FromForm] string title) =>
{

    var id = Guid.NewGuid().ToString();
    Directory.CreateDirectory("image");
    var path = Path.Combine("image", id + Path.GetExtension(file.FileName));
    using var stream = System.IO.File.OpenWrite(path);
    await file.CopyToAsync(stream);

    var jsonPath = Path.Combine("image", "imageDb.json");
    var data = new Dictionary<string, Dictionary<string, string>>();

    data?.Add(id,
        new Dictionary<string, string> {
            { "title", title }, { "path", path }, { "contentType", file.ContentType }
        });
    await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(data));

    return Results.Redirect($"/picture/{id}");
});


app.MapGet("/pictureFile/{id}", async (string id) =>
{
    var data = await GetImageData(id);

    return Results.Stream(async stream =>
    {
        await using var fileStream = File.OpenRead(data["path"]);
        await fileStream.CopyToAsync(stream);
    }, data["contentType"]);
});

app.MapGet("/picture/{id}", async (string id) =>
{
    var data = await GetImageData(id);
    return Results.Content(
            $"""
             <html>
             <head>
                 <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-QWTKZyjpPEjISv5WaRU9OFeRpok6YctnYmDr5pNlyT2bRjXh0JMhjY6hW+ALEwIH" crossorigin="anonymous">
             </head>
             <body>
                 <div class="container">
                     <h1 style="text-align:center">{data["title"]}</h1>
                     <img src="/pictureFile/{id}" class="img-fluid" width=500 />
                 </div>
             </body>
             </html>
             """, contentType: "text/html");
});


app.Run();
return;


async Task<Dictionary<string, string>?> GetImageData(string id)
{

    var jsonPath = Path.Combine("image", "imageDb.json");
    var json = File.ReadAllText(jsonPath);
    var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);

    return data?[id];
}

