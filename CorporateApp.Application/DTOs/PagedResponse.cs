using System.Collections.Generic;

namespace CorporateApp.Application.DTOs
{
    public class PagedResponse<T>
    {
        public IEnumerable<T> Data { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages 
        { 
            get 
            { 
                if (PageSize == 0) return 0;
                return (TotalCount + PageSize - 1) / PageSize; 
            } 
        }
        public bool HasPreviousPage 
        { 
            get { return PageNumber > 1; } 
        }
        public bool HasNextPage 
        { 
            get { return PageNumber < TotalPages; } 
        }

        public PagedResponse()
        {
        }

        public PagedResponse(IEnumerable<T> data, int totalCount, int pageNumber, int pageSize)
        {
            Data = data;
            TotalCount = totalCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}
