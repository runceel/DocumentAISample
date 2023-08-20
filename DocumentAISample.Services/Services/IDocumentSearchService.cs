using DocumentAISample.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentAISample.Services;
public interface IDocumentSearchService
{
    ValueTask<DocumentSearchServiceSearchResult> SearchAsync(string keyword, CancellationToken cancellationToken = default);
}

public record DocumentSearchServiceSearchResult(IReadOnlyList<DocumentSearchServiceDocument> Documents);
public record DocumentSearchServiceDocument(
    string DocumentName,
    Uri Uri,
    string Text,
    IReadOnlyList<int> PageNumbers,
    double Score);