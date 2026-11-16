using System.Linq;
using System.Web.Mvc;
using _25DH110847_MyStore.Models;

namespace _25DH110847_MyStore.Areas.Admin.Controllers
{
    public class HomeAdminController : Controller
    {
        private readonly MyStoreEntities db = new MyStoreEntities();

        public ActionResult Index()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account", new { area = "" });

            var username = User.Identity.Name;
            var user = db.Users.SingleOrDefault(u => u.Username == username);
            if (user == null || (user.UserRole ?? string.Empty).Trim() != "A")
                return RedirectToAction("Login", "Account", new { area = "" });

            ViewBag.TotalOrders = db.Orders.Count();
            ViewBag.TotalCustomers = db.Customers.Count();
            ViewBag.TotalProducts = db.Products.Count();
            ViewBag.TotalOrderValue = db.Orders
                .Select(o => (decimal?)o.TotalAmount)
                .Sum() ?? 0m;

            var statuses = db.Orders.Select(o => o.OrderStatus).ToList();
            ViewBag.PendingOrders = statuses.Count(s => s == "Chờ thanh toán");
            ViewBag.ProcessingOrders = statuses.Count(s => s == "Đang xử lý");
            ViewBag.ShippingOrders = statuses.Count(s => s == "Đang vận chuyển");
            ViewBag.DeliveredOrders = statuses.Count(s => s == "Đã giao");
            ViewBag.CancelledOrders = statuses.Count(s => s == "Đã hủy");

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}
