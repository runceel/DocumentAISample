using DocumentAISample.Repositories;
using DocumentAISample.Services.Implementations;
using ImportDocumentFunctionApp.Services;
using Microsoft.Extensions.Options;

namespace DocumentAISample.Services.Implements;

public class ImportDocumentServiceTest
{
    [Fact]
    public async Task ImportTest()
    {
        // arrange
        var documentService = Substitute.For<IDocumentParseService>();
        documentService.ParseDocumentAsync(
            Arg.Any<ParseDocumentInput>())
            .Returns(ValueTask.FromResult(new ParseDocumentResult(new[]
            {
                new PageContent(1, "abc"),
                new PageContent(2, "def"),
                new PageContent(3, "ghi"),
            })));

        var embeddingsService = Substitute.For<IEmbeddingsService>();
        embeddingsService.GenerateEmbeddingsAsync(Arg.Any<GenerateEmbeddingsInput>())
            .Returns(
                ValueTask.FromResult(new GenerateEmbeddingsResult(new[]
                {
                    1.1f, 1.2f, 1.3f,
                })),
                ValueTask.FromResult(new GenerateEmbeddingsResult(new[]
                {
                    1.4f, 1.5f, 1.6f,
                })));

        var blobService = Substitute.For<IBlobService>();
        blobService.CopyFromUriAsync(new Uri("https://example.com"),
            "export",
            "test.pdf")
            .Returns(ValueTask.CompletedTask);

        InsertTargetDocument? insertDocumentAsyncArg = default;
        var searchService = Substitute.For<IDocumentRepository>();
        searchService.InsertDocumentAsync(
            Arg.Do<InsertTargetDocument>(x => insertDocumentAsyncArg = x))
            .Returns(ValueTask.CompletedTask);

        // act
        var target = new ImportDocumentService(
            documentService,
            embeddingsService,
            blobService,
            searchService,
            Options.Create(new ImportDocumentServiceOptions
            {
                ExportContainerName = "export",
                PageSize = 2,
            }));
        await target.ImportAsync(new ImportTarget(
            "test.pdf", new Uri("https://example.com")));

        // assert
        await documentService.Received(1).ParseDocumentAsync(
            new ParseDocumentInput(new Uri("https://example.com")));
        await embeddingsService.Received()
            .GenerateEmbeddingsAsync(Arg.Is<GenerateEmbeddingsInput>(x => x.Text == "abc def"));
        await embeddingsService.Received()
            .GenerateEmbeddingsAsync(Arg.Is<GenerateEmbeddingsInput>(x => x.Text == "ghi"));

        await blobService.Received(1).CopyFromUriAsync(
            new Uri("https://example.com"),
            "export",
            "test.pdf");

        Assert.Equal("test.pdf", insertDocumentAsyncArg?.DocumentName);
        Assert.Collection(insertDocumentAsyncArg?.DocumentChunks!,
            x =>
            {
                Assert.Equal(new[] { 1, 2 }, x.PageNumbers);
                Assert.Equal("abc def", x.Text);
                Assert.Equal(new[] { 1.1f, 1.2f, 1.3f }, x.Embeddings);
            },
            x =>
            {
                Assert.Equal(new[] { 3 }, x.PageNumbers);
                Assert.Equal("ghi", x.Text);
                Assert.Equal(new[] { 1.4f, 1.5f, 1.6f }, x.Embeddings);
            });
    }
}
