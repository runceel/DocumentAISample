using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImportDocumentFunctionApp.Services;
internal class ImportDocumentServiceOptions
{
    public string SearchIndexName { get; set; } = "documents";
    public string DocumentAnalysisModelId { get; } = "prebuilt-document";
    public string EmbeddingModelName { get; set; } = "";
    public string ExportContainerName { get; set; } = "";
    public int PageSize { get; set; } = 3;
}
