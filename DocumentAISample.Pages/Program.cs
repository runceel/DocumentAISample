using Azure.Core;
using Azure.Identity;
using DocumentAISample.Pages.Data;
using DocumentAISample.Repositories;
using DocumentAISample.Services;
using DocumentAISample.Services.Implementations;
using DocumentAISample.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddAzureServices(builder.Environment, builder.Configuration);
builder.Services.AddApplicationOptions();
builder.Services.AddApplicationServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationOptions(this IServiceCollection services)
    {
        services.AddOptions<AzureEmbeddingsServiceOptions>()
            .BindConfiguration(nameof(AzureEmbeddingsServiceOptions))
            .ValidateDataAnnotations();
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton(ISystemClock.Instance);
        services.AddSingleton<IDocumentSearchService, DocumentSearchService>();
        services.AddSingleton<IEmbeddingsService, AzureEmbeddingsService>();
        services.AddSingleton<IDocumentRepository, AzureSearchDocumentRepository>();
        services.AddSingleton<IBlobService, AzureBlobService>();
        return services;
    }

    public static IServiceCollection AddAzureServices(this IServiceCollection services, IWebHostEnvironment environment, IConfiguration configuration)
    {
        static TokenCredential createCredential(IWebHostEnvironment environment)
        {
            if (environment.IsDevelopment())
            {
                return new AzureCliCredential();
            }

            return new DefaultAzureCredential();
        }

        var credential = createCredential(environment);
        services.AddSingleton(credential);
        services.AddAzureClients(builder =>
        {
            builder.AddSearchIndexClient(configuration.GetSection("Search"));
            builder.AddOpenAIClient(configuration.GetSection("OpenAI"));
            builder.AddBlobServiceClient(configuration.GetSection("OutputBlobService"));
            builder.UseCredential(credential);
        });

        return services;
    }
}