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
        public int CurrentPage { get; set; }
        public int TotalData { get; set; }
        public int TotalPage { get; set; }
        public int PageSize { get; set; }
        public string SearchString { get; set; }
        public string SortField { get; set; }
        public string SortOrder { get; set; }
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

        public QueryOptions QueryOptions { get; private set; }
        public List<T> Results { get; private set; }
    }
}