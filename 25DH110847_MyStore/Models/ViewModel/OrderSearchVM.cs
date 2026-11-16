using System;
using PagedList;

namespace _25DH110847_MyStore.Models.ViewModel
{
    public class OrderSearchVM
    {
        public string CustomerName { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime? OrderDate { get; set; }
        public string SortOrder { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; } = 5;
        public IPagedList<Order> Orders { get; set; }
    }
}
