using ImageConverter;
using ImageConverter.Models.Encoding;
using ImageConverter.Models.Formats;
using ImageConverter.Models.Imaging;
using ImageConverter.Models.Packaging;
using ImageConverter.Models.Session;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<IFormatCatalog, ImageSharpFormatCatalog>();
builder.Services.AddSingleton<IImageFormatEncoderFactory, ImageSharpEncoderFactory>();
builder.Services.AddSingleton<IImagePreviewBuilder, ImageSharpImagePreviewBuilder>();
builder.Services.AddSingleton<IImagePackageBuilder, ZipImagePackageBuilder>();
builder.Services.AddScoped<IConversionSession, ConversionSession>();
builder.Services.AddMudServices();

await builder.Build().RunAsync();
