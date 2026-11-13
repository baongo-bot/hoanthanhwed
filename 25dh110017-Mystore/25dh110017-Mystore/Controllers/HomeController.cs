using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using PagedList;
using System.Data.Entity; // Bổ sung namespace này để dùng .Include() chuẩn xác
using _25dh110017_Mystore.Models;
using _25dh110017_Mystore.Models.ViewModel;

namespace _25dh110017_Mystore.Controllers
{
    public class HomeController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        // GET: Home/Index
        public ActionResult Index(int? categoryId, string searchTerm, int? page)
        {
            var model = new HomeProductVM();

            // Giữ lại từ khóa tìm kiếm trên giao diện
            model.SearchTerm = searchTerm;

            // Lấy toàn bộ sản phẩm và nạp sẵn (Include) bảng Category để tránh lỗi null khi tìm kiếm
            var products = db.Products.Include(p => p.Category).AsQueryable();

            // 1. Lọc theo danh mục
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                products = products.Where(
                    p => p.CategoryID == categoryId.Value
                );
            }

            // 2. Tìm kiếm theo tên, mô tả hoặc danh mục
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string keyword = searchTerm.Trim();

                products = products.Where(p =>
                    (p.ProductName != null &&
                     p.ProductName.Contains(keyword)) ||

                    (p.ProductDescription != null &&
                     p.ProductDescription.Contains(keyword)) ||

                    (p.Category != null &&
                     p.Category.CategoryName != null &&
                     p.Category.CategoryName.Contains(keyword))
                );
            }

            // 3. Sản phẩm nổi bật
            model.FeaturedProducts = products
                .OrderByDescending(p => p.ProductID)
                .Take(10)
                .ToList();

            // 4. Phân trang sản phẩm
            int pageNumber = page ?? 1;
            int pageSize = 6;

            model.NewProducts = products
                .OrderByDescending(p => p.ProductID)
                .ToPagedList(pageNumber, pageSize);

            // 5. Giữ danh mục đang được chọn để sáng nút Active trên giao diện
            ViewBag.SelectedCategory = categoryId;

            return View(model);
        }

        // GET: Home/ProductDetail/5
        public ActionResult ProductDetail(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest
                );
            }

            Product product = db.Products.Find(id);

            if (product == null)
            {
                return HttpNotFound();
            }

            return View(product);
        }

        // GET: Home/About
        public ActionResult About()
        {
            ViewBag.Message = "Trang giới thiệu thông tin nhà sách.";
            return View();
        }

        // GET: Home/Contact
        public ActionResult Contact()
        {
            ViewBag.Message = "Trang liên hệ hỗ trợ khách hàng.";
            return View();
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