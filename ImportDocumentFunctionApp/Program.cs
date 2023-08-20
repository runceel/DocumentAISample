using Azure.Identity;
using DocumentAISample.Services;
using DocumentAISample.Services.Implementations;
using ImportDocumentFunctionApp.Options;
using ImportDocumentFunctionApp.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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
        services.AddOptions<CosmosDb>()
           .BindConfiguration(nameof(CosmosDb))
           .ValidateDataAnnotations();
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
        services.AddSingleton<IDocumentService, AzureDocumentService>();
        services.AddSingleton<IEmbeddingsService, AzureEmbeddingsService>();
        services.AddSingleton<IBlobService, AzureBlobService>();
        services.AddSingleton<ISearchService, AzureSearchService>();
        services.AddSingleton<IImportDocumentService, ImportDocumentService>();
        return services;
    }

    public static IServiceCollection AddAzureServices(this IServiceCollection services, HostBuilderContext context)
    {
        //var credential = new DefaultAzureCredential(options: new()
        //{
        //    ExcludeVisualStudioCredential = true,
        //});

        var credential = new AzureCliCredential();
        services.AddAzureClients(builder =>
        {
            builder.AddSearchIndexClient(context.Configuration.GetSection("Search"));
            builder.AddDocumentAnalysisClient(context.Configuration.GetSection("DocumentAnalysis"));
            builder.AddOpenAIClient(context.Configuration.GetSection("OpenAI"));
            builder.AddBlobServiceClient(context.Configuration.GetSection("OutputBlobService"));
            builder.UseCredential(credential);
        });

        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<CosmosDb>>();
            return new CosmosClient(
                options.Value.Endpoint,
                credential);
        });
        return services;
    }
}