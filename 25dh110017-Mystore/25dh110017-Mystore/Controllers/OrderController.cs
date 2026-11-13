using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web.Mvc;
using _25dh110017_Mystore.Models;
using _25dh110017_Mystore.Models.Cart;
using _25dh110017_Mystore.Models.ViewModel;
// Sử dụng Alias để định nghĩa rõ ràng Class Cart, tránh đụng độ Namespace
using CartClass = _25dh110017_Mystore.Models.Cart.Cart;

namespace _25dh110017_Mystore.Controllers
{
    public class OrderController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        private CartService GetCartService()
        {
            return new CartService(Session);
        }

        // 1. TRANG ĐẶT HÀNG (GET: Order/Checkout)
        public ActionResult Checkout()
        {
            var cart = GetCartService().GetCart();
            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var model = new CheckoutVM
            {
                Cart = cart
            };

            // Nếu người dùng đã đăng nhập, tự động lấy thông tin điền sẵn
            if (Session["Username"] != null)
            {
                string username = Session["Username"].ToString();
                var customer = db.Customers.FirstOrDefault(c => c.Username == username);
                if (customer != null)
                {
                    model.CustomerName = customer.CustomerName;
                    model.Phone = customer.CustomerPhone;
                    model.AddressDelivery = customer.CustomerAddress;
                }
            }

            return View("Checkout");
        }

        // 2. XỬ LÝ ĐẶT HÀNG (POST: Order/Checkout)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(CheckoutVM model)
        {
            var cart = GetCartService().GetCart();
            model.Cart = cart;

            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        int customerId = 0;

                        // TRƯỜNG HỢP 1: Đã đăng nhập hệ thống
                        if (Session["Username"] != null)
                        {
                            string username = Session["Username"].ToString();
                            var customer = db.Customers.FirstOrDefault(c => c.Username == username);
                            if (customer != null)
                            {
                                customerId = customer.CustomerID;
                                customer.CustomerName = model.CustomerName;
                                customer.CustomerPhone = model.Phone;
                                customer.CustomerAddress = model.AddressDelivery;
                            }
                        }

                        // TRƯỜNG HỢP 2: Khách vãng lai -> Tự tạo hoặc tìm Customer dựa theo số điện thoại
                        if (customerId == 0)
                        {
                            var existingCustomer = db.Customers.FirstOrDefault(c => c.CustomerPhone == model.Phone);
                            if (existingCustomer != null)
                            {
                                customerId = existingCustomer.CustomerID;
                            }
                            else
                            {
                                Customer newCustomer = new Customer
                                {
                                    CustomerName = model.CustomerName,
                                    CustomerPhone = model.Phone,
                                    CustomerAddress = model.AddressDelivery
                                };
                                db.Customers.Add(newCustomer);
                                db.SaveChanges(); // Lưu để lấy CustomerID vừa sinh tự động
                                customerId = newCustomer.CustomerID;
                            }
                        }

                        // Tạo đối tượng Đơn hàng
                        Order order = new Order
                        {
                            CustomerID = customerId,
                            OrderDate = DateTime.Now,
                            TotalAmount = cart.TotalValue(),
                            AddressDelivery = model.AddressDelivery,
                            PaymentStatus = "Pending"
                        };

                        db.Orders.Add(order);
                        db.SaveChanges(); // Lưu để sinh tự động OrderID

                        // Lưu chi tiết từng sản phẩm trong đơn hàng
                        foreach (var item in cart.Items)
                        {
                            OrderDetail detail = new OrderDetail
                            {
                                OrderID = order.OrderID,
                                ProductID = item.product.ProductID,
                                Quantity = item.quantity,
                                UnitPrice = item.product.ProductPrice
                            };
                            db.OrderDetails.Add(detail);
                        }

                        db.SaveChanges();
                        transaction.Commit();

                        // Đặt hàng thành công -> Xóa giỏ hàng
                        GetCartService().ClearCart();

                        return RedirectToAction("OrderSuccess", new { id = order.OrderID });
                    }
                    catch (DbEntityValidationException ex)
                    {
                        transaction.Rollback();
                        foreach (var entityErrors in ex.EntityValidationErrors)
                        {
                            foreach (var validationError in entityErrors.ValidationErrors)
                            {
                                ModelState.AddModelError("", $"Lỗi CSDL [{validationError.PropertyName}]: {validationError.ErrorMessage}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        string errorMsg = ex.Message;
                        if (ex.InnerException != null)
                        {
                            errorMsg += " -> Chi tiết: " + ex.InnerException.Message;
                            if (ex.InnerException.InnerException != null)
                            {
                                errorMsg += " (" + ex.InnerException.InnerException.Message + ")";
                            }
                        }
                        ModelState.AddModelError("", "Đã xảy ra lỗi khi tạo đơn hàng: " + errorMsg);
                    }
                }
            }

            return View(model);
        }

        // 3. TRANG XÁC NHẬN ĐƠN HÀNG THÀNH CÔNG (GET: Order/OrderSuccess/5)
        public ActionResult OrderSuccess(int id)
        {
            var order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        // 4. LỊCH SỬ ĐƠN HÀNG CỦA TÔI (GET: Order/History)
        public ActionResult History()
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            string username = Session["Username"].ToString();
            var customer = db.Customers.FirstOrDefault(c => c.Username == username);
            if (customer == null)
            {
                return View(new List<Order>());
            }

            // Lấy danh sách đơn hàng của khách hàng hiện tại, sắp xếp mới nhất lên đầu
            var orders = db.Orders.Where(o => o.CustomerID == customer.CustomerID)
                                  .OrderByDescending(o => o.OrderDate)
                                  .ToList();

            return View(orders);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}