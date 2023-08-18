using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImportDocumentFunctionApp.Options;
internal class CosmosDb
{
    [Required]
    public string Endpoint { get; set; } = "";
}
