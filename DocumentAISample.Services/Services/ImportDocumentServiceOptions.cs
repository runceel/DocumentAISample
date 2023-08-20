using System.ComponentModel.DataAnnotations;

namespace ImportDocumentFunctionApp.Services;
public class ImportDocumentServiceOptions
{
    [Required]
    public string ExportContainerName { get; set; } = "";
    public int PageSize { get; set; } = 3;
}
