using System.Collections.Generic;
using _25DH110847_MyStore.Models;

namespace _25DH110847_MyStore.Models.ViewModel
{
    public class MyOrderVM
    {
        public string SearchTerm { get; set; }
        public string ActiveStatus { get; set; }
        public bool ShowMore { get; set; }
        public List<MyOrderStatusTabVM> Tabs { get; set; } = new List<MyOrderStatusTabVM>();
        public List<MyOrderGroupVM> Groups { get; set; } = new List<MyOrderGroupVM>();
    }

    public class MyOrderStatusTabVM
    {
        public string Key { get; set; }
        public string Title { get; set; }
    }

    public class MyOrderGroupVM
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public List<Order> Orders { get; set; } = new List<Order>();
        public bool HasMore { get; set; }
    }
}
