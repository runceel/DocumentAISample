using Azure.Identity;
using ImportDocumentFunctionApp.Options;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Options
        services.AddOptions<CosmosDb>()
            .BindConfiguration(nameof(CosmosDb))
            .ValidateDataAnnotations();

        // Azure SDK
        var credential = new DefaultAzureCredential(options: new()
        {
            ExcludeVisualStudioCredential = true,
        });

        services.AddAzureClients(builder =>
        {
            builder.AddSearchIndexClient(context.Configuration.GetSection("Search"));
            builder.AddDocumentAnalysisClient(context.Configuration.GetSection("DocumentAnalysis"));
            builder.AddOpenAIClient(context.Configuration.GetSection("OpenAI"));
            builder.AddBlobServiceClient(context.Configuration.GetSection("ExportStorage"));
            builder.UseCredential(credential);
        });

        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<CosmosDb>>();
            return new CosmosClient(
                options.Value.Endpoint,
                credential);
        });
    })
    .Build();

host.Run();
