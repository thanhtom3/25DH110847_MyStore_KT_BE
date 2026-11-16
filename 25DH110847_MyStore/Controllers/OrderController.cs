using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using _25DH110847_MyStore.Models;
using _25DH110847_MyStore.Models.ViewModel;

namespace _25DH110847_MyStore.Controllers
{
    public class OrderController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        private static readonly KeyValuePair<string, string>[] OrderStatuses =
        {
            new KeyValuePair<string, string>("pending", "Chờ thanh toán"),
            new KeyValuePair<string, string>("processing", "Đang xử lý"),
            new KeyValuePair<string, string>("shipping", "Đang vận chuyển"),
            new KeyValuePair<string, string>("delivered", "Đã giao"),
            new KeyValuePair<string, string>("cancelled", "Đã hủy")
        };

        public ActionResult Index()
        {
            return RedirectToAction("OrderHistory");
        }

        // GET: Order/OrderHistory
        [Authorize]
        public ActionResult OrderHistory(string searchTerm, string status, bool showMore = false)
        {
            var customer = db.Customers.SingleOrDefault(c => c.Username == User.Identity.Name);
            if (customer == null)
                return RedirectToAction("Login", "Account");

            var model = new CustomerOrderHistoryVM
            {
                SearchTerm = searchTerm,
                ActiveStatus = string.IsNullOrWhiteSpace(status) ? "all" : status,
                ShowMore = showMore
            };
            model.Tabs.Add(new CustomerOrderStatusTabVM { Key = "all", Title = "Tất cả đơn" });
            model.Tabs.AddRange(OrderStatuses.Select(s => new CustomerOrderStatusTabVM { Key = s.Key, Title = s.Value }));

            if (model.ActiveStatus != "all" && !OrderStatuses.Any(s => s.Key == model.ActiveStatus))
                model.ActiveStatus = "all";

            var orders = db.Orders
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .Where(o => o.CustomerID == customer.CustomerID);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                int orderId;
                if (int.TryParse(term, out orderId))
                {
                    orders = orders.Where(o => o.OrderID == orderId ||
                        o.OrderDetails.Any(d => d.Product.ProductName.Contains(term)));
                }
                else
                {
                    orders = orders.Where(o =>
                        o.OrderDetails.Any(d => d.Product.ProductName.Contains(term)));
                }
            }

            var customerOrders = orders.OrderByDescending(o => o.OrderDate).ToList();
            var selectedStatuses = model.ActiveStatus == "all"
                ? OrderStatuses.ToList()
                : OrderStatuses.Where(s => s.Key == model.ActiveStatus).ToList();

            foreach (var orderStatus in selectedStatuses)
            {
                var matchingOrders = customerOrders
                    .Where(o => o.OrderStatus == orderStatus.Value)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                if (!matchingOrders.Any())
                    continue;

                var limit = model.ActiveStatus != "all" && showMore ? 50 : 2;
                model.Groups.Add(new CustomerOrderHistoryGroupVM
                {
                    Key = orderStatus.Key,
                    Title = orderStatus.Value,
                    Orders = matchingOrders.Take(limit).ToList(),
                    HasMore = model.ActiveStatus == "all" && matchingOrders.Count > 2
                        || model.ActiveStatus == orderStatus.Key && !showMore && matchingOrders.Count > 2
                });
            }

            return View(model);
        }

        // POST: Order/BuyAgain/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult BuyAgain(int id)
        {
            var customer = db.Customers.SingleOrDefault(c => c.Username == User.Identity.Name);
            if (customer == null)
                return RedirectToAction("Login", "Account");

            var order = db.Orders
                .Include(o => o.OrderDetails.Select(d => d.Product.Category))
                .SingleOrDefault(o => o.OrderID == id && o.CustomerID == customer.CustomerID);

            if (order == null)
                return HttpNotFound();

            var cart = new CartService(Session).GetCart();
            foreach (var item in order.OrderDetails)
            {
                if (item.Product == null)
                    continue;

                cart.AddItem(item.Product.ProductID, item.Product.ProductImage,
                    item.Product.ProductName, item.Product.ProductPrice, 1,
                    item.Product.Category == null ? string.Empty : item.Product.Category.CategoryName);
            }

            return RedirectToAction("Index", "Cart");
        }

        // GET: Order/Checkout
        [Authorize]
        public ActionResult Checkout()
        {
            var cartItems = new CartService(Session).GetCart().Items.ToList();
            if (!cartItems.Any())
                return RedirectToAction("Index", "Cart");

            var user = db.Users.SingleOrDefault(u => u.Username == User.Identity.Name);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var customer = db.Customers.SingleOrDefault(c => c.Username == user.Username);
            if (customer == null)
                return RedirectToAction("Login", "Account");

            var model = new CheckoutVM
            {
                CartItems = cartItems,
                TotalAmount = cartItems.Sum(item => item.TotalPrice),
                OrderDate = DateTime.Now,
                ShippingAddress = customer.CustomerAddress,
                CustomerID = customer.CustomerID,
                Username = customer.Username
            };

            return View(model);
        }

        // POST: Order/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Checkout(CheckoutVM model)
        {
            var cartItems = new CartService(Session).GetCart().Items.ToList();
            if (!cartItems.Any())
                return RedirectToAction("Index", "Cart");

            if (!ModelState.IsValid)
            {
                model.CartItems = cartItems;
                model.TotalAmount = cartItems.Sum(item => item.TotalPrice);
                return View(model);
            }

            var user = db.Users.SingleOrDefault(u => u.Username == User.Identity.Name);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var customer = db.Customers.SingleOrDefault(c => c.Username == user.Username);
            if (customer == null)
                return RedirectToAction("Login", "Account");

            if (model.PaymentMethod == "Paypal")
                return RedirectToAction("PaymentWithPaypal", "PayPal", model);

            var paymentStatus = "Chưa thanh toán";
            switch (model.PaymentMethod)
            {
                case "Tiền mặt": paymentStatus = "Thanh toán tiền mặt"; break;
                case "Paypal": paymentStatus = "Thanh toán paypal"; break;
                case "Mua trước trả sau": paymentStatus = "Trả góp"; break;
            }

            var order = new Order
            {
                CustomerID = customer.CustomerID,
                OrderDate = DateTime.Now,
                TotalAmount = cartItems.Sum(item => item.TotalPrice),
                PaymentStatus = paymentStatus,
                OrderStatus = "Đang xử lý",
                PaymentMethod = model.PaymentMethod,
                DeliveryMethod = model.ShippingMethod,
                ShippingAddress = model.ShippingAddress,
                OrderDetails = cartItems.Select(item => new OrderDetail
                {
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                }).ToList()
            };

            db.Orders.Add(order);
            db.SaveChanges();
            Session["Cart"] = null;

            return RedirectToAction("OrderSuccess", new { id = order.OrderID });
        }

        [Authorize]
        public ActionResult OrderSuccess(int id)
        {
            var customer = db.Customers.SingleOrDefault(c => c.Username == User.Identity.Name);
            if (customer == null)
                return RedirectToAction("Login", "Account");

            var order = db.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .SingleOrDefault(o => o.OrderID == id && o.CustomerID == customer.CustomerID);

            if (order == null)
                return HttpNotFound();

            return View(order);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}
