using Edubase.Services.Enums;

namespace Edubase.Web.UI.Models.Search
{
    public class DownloadErrorViewModel
    {
        public string SearchQueryString { get; set; }
        public eLookupSearchSource? SearchSource { get; set; }
    }
}
