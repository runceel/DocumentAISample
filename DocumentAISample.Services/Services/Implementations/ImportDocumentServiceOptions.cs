using System.ComponentModel.DataAnnotations;

namespace DocumentAISample.Services.Implementations;
public class ImportDocumentServiceOptions
{
    [Required]
    public string ExportContainerName { get; set; } = "";
    public int PageSize { get; set; } = 3;
}
