using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryFramework.Models.Paging
{
    public enum SortOrder
    {
        ASC,
        DESC
    }

    public class QueryOptions
    {
        public QueryOptions()
        {
            CurrentPage = 1;
            PageSize = 1;
            SortField = "Id";
            SortOrder = Paging.SortOrder.ASC.ToString();
        }

        [JsonProperty(PropertyName = "currentPage")]
        public int CurrentPage { get; set; }
        [JsonProperty(PropertyName = "totalData")]
        public int TotalData { get; set; }
        [JsonProperty(PropertyName = "totalPages")]
        public int TotalPages { get; set; }
        [JsonProperty(PropertyName = "pageSize")]
        public int PageSize { get; set; }
        [JsonProperty(PropertyName = "searchString")]
        public string SearchString { get; set; }
        [JsonProperty(PropertyName = "sortField")]
        public string SortField { get; set; }
        [JsonProperty(PropertyName = "sortOrder")]
        public string SortOrder { get; set; }

        [JsonIgnore]
        public string Sort
        {
            get
            {
                return string.Format("{0} {1}", SortField, SortOrder);
            }
        }
    }

    public class ResultList<T>
    {
        public ResultList(List<T> results, QueryOptions queryOptions)
        {
            Results = results;
            QueryOptions = queryOptions;
        }

        [JsonProperty(PropertyName = "queryOptions")]
        public QueryOptions QueryOptions { get; private set; }
        [JsonProperty(PropertyName = "results")]
        public List<T> Results { get; private set; }
    }
}