// File: Program.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR;
using System.Linq;
using CSharpCraft.Clases;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<PlayerManager>();
builder.Services.AddSingleton<Physics>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<ChunkService>();
//builder.Services.AddScoped<GameStateService>();


builder.Services.AddScoped<InteropHelper>();


builder.Services.AddControllers();

// Add response compression for SignalR
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/octet-stream" });
});

// In ConfigureServices:
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", builder =>
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader());
});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();


app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true, // Allow serving unknown file types
    DefaultContentType = "application/octet-stream",
    ContentTypeProvider = new FileExtensionContentTypeProvider
    {
        Mappings =
            {
                [".glb"] = "model/gltf-binary"
            }
    }
});

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapBlazorHub();
    endpoints.MapHub<GameHub>("/gameHub"); // Ensure you have created the GameHub class
    endpoints.MapFallbackToPage("/_Host");
});

app.MapControllers();

// In Configure:
app.UseCors("AllowAll");

app.UseAuthorization(); // Ensure this line is present before endpoint mapping

app.Run();
