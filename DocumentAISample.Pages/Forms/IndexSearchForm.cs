using System.ComponentModel.DataAnnotations;

namespace DocumentAISample.Pages.Forms;

public class IndexSearchForm
{
    [Required(ErrorMessage = "Please enter keywords")]
    public string Keywords { get; set; } = "";
}
