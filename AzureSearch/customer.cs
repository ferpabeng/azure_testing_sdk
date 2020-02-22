using Microsoft.Azure.Search;

namespace AzureSearch
{
    public partial class customer
    {
        [System.ComponentModel.DataAnnotations.Key]
        public string Id { get; set; }
        [IsFilterable]
        public string Name { get; set; }
        [IsFilterable, IsSortable, IsFacetable]
        public string Course { get; set; }
        [IsSearchable]
        public string Comment { get; set; }

        [IsSortable]
        public string Progress { get; set; }
    }
}
