using System.Collections.Generic;
using _25DH110847_MyStore.Models;

namespace _25DH110847_MyStore.Models.ViewModel
{
    public class CustomerOrderHistoryVM
    {
        public string SearchTerm { get; set; }
        public string ActiveStatus { get; set; }
        public bool ShowMore { get; set; }
        public List<CustomerOrderStatusTabVM> Tabs { get; set; } = new List<CustomerOrderStatusTabVM>();
        public List<CustomerOrderHistoryGroupVM> Groups { get; set; } = new List<CustomerOrderHistoryGroupVM>();
    }

    public class CustomerOrderStatusTabVM
    {
        public string Key { get; set; }
        public string Title { get; set; }
    }

    public class CustomerOrderHistoryGroupVM
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public List<Order> Orders { get; set; } = new List<Order>();
        public bool HasMore { get; set; }
    }
}
