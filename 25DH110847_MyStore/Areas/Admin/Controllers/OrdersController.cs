using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using _25DH110847_MyStore.Models;
using _25DH110847_MyStore.Models.ViewModel;
using PagedList;

namespace _25DH110847_MyStore.Areas.Admin.Controllers
{
    public class OrdersController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();
        private readonly string[] orderStatuses =
        {
            "Chờ thanh toán", "Đang xử lý", "Đang vận chuyển", "Đã giao", "Đã hủy"
        };

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!User.Identity.IsAuthenticated)
            {
                filterContext.Result = RedirectToAction("Login", "Account", new { area = "" });
                return;
            }

            var username = User.Identity.Name;
            var user = db.Users.SingleOrDefault(u => u.Username == username);
            if (user == null || (user.UserRole ?? string.Empty).Trim() != "A")
            {
                filterContext.Result = RedirectToAction("Login", "Account", new { area = "" });
                return;
            }

            base.OnActionExecuting(filterContext);
        }

        // GET: Admin/Orders
        public ActionResult Index(string customerName, string paymentStatus, DateTime? orderDate,
            string sortOrder, int? page)
        {
            var model = new OrderSearchVM();
            var orders = db.Orders.Include(o => o.Customer).AsQueryable();

            if (!string.IsNullOrWhiteSpace(customerName))
            {
                customerName = customerName.Trim();
                orders = orders.Where(o => o.Customer.CustomerName.Contains(customerName));
            }

            if (!string.IsNullOrWhiteSpace(paymentStatus))
                orders = orders.Where(o => o.PaymentStatus == paymentStatus);

            switch (sortOrder)
            {
                case "date_asc":
                    orders = orders.OrderBy(o => o.OrderDate);
                    break;
                case "customer_asc":
                    orders = orders.OrderBy(o => o.Customer.CustomerName);
                    break;
                case "customer_desc":
                    orders = orders.OrderByDescending(o => o.Customer.CustomerName);
                    break;
                case "amount_asc":
                    orders = orders.OrderBy(o => o.TotalAmount);
                    break;
                case "amount_desc":
                    orders = orders.OrderByDescending(o => o.TotalAmount);
                    break;
                default:
                    sortOrder = "date_desc";
                    orders = orders.OrderByDescending(o => o.OrderDate);
                    break;
            }

            model.CustomerName = customerName;
            model.PaymentStatus = paymentStatus;
            model.OrderDate = orderDate;
            model.SortOrder = sortOrder;
            model.PageNumber = page ?? 1;
            if (orderDate.HasValue)
            {
                var startDate = orderDate.Value.Date;
                var endDate = startDate.AddDays(1);
                orders = orders.Where(o =>
                    o.OrderDate >= startDate && o.OrderDate < endDate);
            }

            model.Orders = orders.ToPagedList(Math.Max(model.PageNumber, 1), model.PageSize);
            return View(model);
        }

        // GET: Admin/Orders/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var order = db.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .SingleOrDefault(o => o.OrderID == id.Value);

            if (order == null)
                return HttpNotFound();

            return View(order);
        }

        // GET: Admin/Orders/EditStatus/5
        public ActionResult EditStatus(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var order = db.Orders.Find(id);
            if (order == null)
                return HttpNotFound();

            ViewBag.OrderStatus = new SelectList(orderStatuses, order.OrderStatus);
            return View(order);
        }

        // POST: Admin/Orders/EditStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditStatus(int id, string orderStatus)
        {
            var order = db.Orders.Find(id);
            if (order == null)
                return HttpNotFound();

            if (!orderStatuses.Contains(orderStatus))
            {
                ModelState.AddModelError("orderStatus", "Trạng thái đơn hàng không hợp lệ.");
                ViewBag.OrderStatus = new SelectList(orderStatuses, order.OrderStatus);
                return View(order);
            }

            order.OrderStatus = orderStatus;
            db.SaveChanges();
            return RedirectToAction("Details", new { id = order.OrderID });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}
