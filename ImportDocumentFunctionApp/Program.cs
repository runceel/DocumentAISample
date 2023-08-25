using Azure.Core;
using Azure.Identity;
using DocumentAISample.Repositories;
using DocumentAISample.Services;
using DocumentAISample.Services.Implementations;
using DocumentAISample.Utils;
using ImportDocumentFunctionApp.Services;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, builder) =>
    {
        if (context.HostingEnvironment.IsDevelopment())
        {
            builder.AddUserSecrets<Program>();
        }
    })
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationOptions();
        services.AddAzureServices(context);
        services.AddApplicationServices();
    })
    .Build();

host.Run();

static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationOptions(this IServiceCollection services)
    {
        services.AddOptions<ImportDocumentServiceOptions>()
            .BindConfiguration(nameof(ImportDocumentServiceOptions))
            .ValidateDataAnnotations();
        services.AddOptions<AzureEmbeddingsServiceOptions>()
            .BindConfiguration(nameof(AzureEmbeddingsServiceOptions))
            .ValidateDataAnnotations();
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton(ISystemClock.Instance);
        services.AddSingleton<IDocumentParseService, AzureDocumentParseService>();
        services.AddSingleton<IEmbeddingsService, AzureEmbeddingsService>();
        services.AddSingleton<IBlobService, AzureBlobService>();
        services.AddSingleton<IDocumentRepository, AzureSearchDocumentRepository>();
        services.AddSingleton<IImportDocumentService, ImportDocumentService>();
        return services;
    }

    public static IServiceCollection AddAzureServices(this IServiceCollection services, HostBuilderContext context)
    {
        static TokenCredential createCredential(HostBuilderContext context)
        {
            if (context.HostingEnvironment.IsDevelopment())
            {
                return new AzureCliCredential();
            }

            return new DefaultAzureCredential();
        }

        var credential = createCredential(context);
        services.AddSingleton(credential);
        services.AddAzureClients(builder =>
        {
            builder.AddSearchIndexClient(context.Configuration.GetSection("Search"));
            builder.AddDocumentAnalysisClient(context.Configuration.GetSection("DocumentAnalysis"));
            builder.AddOpenAIClient(context.Configuration.GetSection("OpenAI"));
            builder.AddBlobServiceClient(context.Configuration.GetSection("OutputBlobService"));
            builder.UseCredential(credential);
        });

        return services;
    }
}