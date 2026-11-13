using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using _25dh110017_Mystore.Models; // Namespace kết nối model của dự án

namespace _25dh110017_Mystore.Areas.Admin.Controllers
{
    public class OrdersController : Controller
    {
        // Khởi tạo kết nối cơ sở dữ liệu Entity Framework
        private MyStoreEntities db = new MyStoreEntities();

        // 1. GET: Admin/Orders (Danh sách đơn hàng)
        public ActionResult Index()
        {
            // Lấy danh sách toàn bộ đơn hàng kèm theo thông tin khách hàng, sắp xếp mới nhất lên đầu
            var orders = db.Orders.Include(o => o.Customer).OrderByDescending(o => o.OrderDate).ToList();
            return View(orders);
        }

        // 2. GET: Admin/Orders/Details/5 (Chi tiết đơn hàng)
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Tìm đơn hàng theo ID kèm thông tin khách hàng
            Order order = db.Orders.Include(o => o.Customer).FirstOrDefault(o => o.OrderID == id);
            if (order == null)
            {
                return HttpNotFound();
            }

            // Lấy danh sách sản phẩm chi tiết thuộc đơn hàng này
            var orderDetails = db.OrderDetails.Include(od => od.Product).Where(od => od.OrderID == id).ToList();
            ViewBag.OrderDetails = orderDetails;

            return View(order);
        }

        // 3. GET: Admin/Orders/Edit/5 (Giao diện chỉnh sửa đơn hàng)
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Order order = db.Orders.Include(o => o.Customer).FirstOrDefault(o => o.OrderID == id);
            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        // 4. POST: Admin/Orders/Edit/5 (Xử lý cập nhật đơn hàng)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "OrderID,CustomerID,OrderDate,TotalAmount,AddressDelivery,PaymentStatus")] Order order)
        {
            if (ModelState.IsValid)
            {
                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(order);
        }

        // 5. GET: Admin/Orders/Delete/5 (Giao diện xác nhận xóa đơn hàng)
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Order order = db.Orders.Include(o => o.Customer).FirstOrDefault(o => o.OrderID == id);
            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        // 6. POST: Admin/Orders/Delete/5 (Xử lý xóa đơn hàng và chi tiết đơn hàng)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Order order = db.Orders.Find(id);
            if (order != null)
            {
                // Xóa tất cả các chi tiết đơn hàng liên quan trước để tránh lỗi ràng buộc khóa ngoại (Foreign Key)
                var orderDetails = db.OrderDetails.Where(od => od.OrderID == id).ToList();
                db.OrderDetails.RemoveRange(orderDetails);

                // Xóa đơn hàng chính
                db.Orders.Remove(order);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        // 7. XÁC NHẬN ĐƠN HÀNG (Admin/Orders/ConfirmOrder/5)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmOrder(int id)
        {
            var order = db.Orders.Find(id);
            if (order != null)
            {
                // Cập nhật trạng thái thanh toán hoặc trạng thái đơn hàng thành "Đã xác nhận" / "Completed"
                order.PaymentStatus = "Đã xác nhận";
                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}