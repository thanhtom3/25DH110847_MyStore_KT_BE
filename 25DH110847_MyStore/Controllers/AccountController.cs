using _25DH110847_MyStore.Models;
using _25DH110847_MyStore.Models.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace _25DH110847_MyStore.Controllers
{
    public class AccountController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        // GET: Account
        public ActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterVM model)
        {
            if (ModelState.IsValid)
            {
                //kiểm tra tên đăng nhập tồn tại chx
                var existingUser = db.Users.SingleOrDefault(u => u.Username == model.Username);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại!");
                    return View(model);
                }

                //nếu chx tồn tại tạo bản ghi thông tin tài khoản trong bảng User
                var user = new User
                {
                    Username = model.Username,
                    Password = model.Password, // Lưu ý: nên mã hóa mật khẩu trước khi lưu
                    // UserRole trong EDMX có độ dài một ký tự: A = Admin, C = Customer.
                    UserRole = "C"
                };
                db.Users.Add(user);

                //tạo bản ghi thông tin khách hàng trong bảng Customer
                var customer = new Customer
                {
                    CustomerName = model.CustomerName,
                    CustomerEmail = model.CustomerEmail,
                    CustomerPhone = model.CustomerPhone,
                    CustomerAddress = model.CustomerAddress,
                    Username = model.Username,
                };
                db.Customers.Add(customer);

                db.SaveChanges();
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        // GET: Account/Login
        public ActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginVM model)
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.SingleOrDefault(u => u.Username == model.Username
                            && u.Password == model.Password);
                if (user != null)
                {
                    var role = (user.UserRole ?? string.Empty).Trim();
                    var isAdmin = string.Equals(role, "A", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
                    var isCustomer = string.Equals(role, "C", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(role, "Customer", StringComparison.OrdinalIgnoreCase);

                    if (!isAdmin && !isCustomer)
                    {
                        ModelState.AddModelError("", "Tài khoản chưa được gán quyền truy cập hợp lệ.");
                        return View(model);
                    }

                    // Lưu trạng thái đăng nhập vào session
                    Session["Username"] = user.Username;
                    Session["UserRole"] = isAdmin ? "Admin" : "Customer";

                    // Lưu thông tin xác thực người dùng vào cookie
                    FormsAuthentication.SetAuthCookie(user.Username, false);

                    return isAdmin
                        ? RedirectToAction("Index", "HomeAdmin", new { area = "Admin" })
                        : RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                }
            }

            // Nếu model không hợp lệ, trả về view kèm theo dữ liệu model
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Index", "Home");
        }
    }
}
