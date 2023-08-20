using System.ComponentModel.DataAnnotations;

namespace ImportDocumentFunctionApp.Options;
internal class CosmosDb
{
    [Required]
    public string Endpoint { get; set; } = "";
}
