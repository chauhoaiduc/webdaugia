using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebDauGia.Filters;
using WebDauGia.Helper;
using WebDauGia.Models;

namespace WebDauGia.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        /// Kiểm tra sản phẩm có phải lá sp của user yêu thích không ?
        /// </summary>
        /// <param name="productID">id sản phẩm</param>
        /// <returns>true : nếu là sản phẩm user yêu thích
        /// false: ngược lại 
        /// và truyền về _FavouritesPartialView</returns>
        [CheckLogin]
        public ActionResult CheckFavourites(int productID)
        {
            var userID = CurrentContext.GetUser().ID;
            using (var db = new WebDauGiaEntities())
            {
                var query = db.WatchLists.Where(w => w.UserID == userID && w.ProductID == productID).FirstOrDefault();
                bool liked = false;
                if (query != null)
                {
                    liked = true;
                }
                return PartialView("_FavouritesPartialView",liked);
            }
                
        }

        /// <summary>
        /// Lấy Top 5 sản phẩm có nhiều lượt ra giá nhất
        /// </summary>
        /// <returns> List<ProductHomeViewModel> </returns>
        public List<ProductHomeViewModel> GetTopAuction()
        {
            using (var db = new WebDauGiaEntities())
            {
                // truy vấn lấy ra ID sản phẩm và số lượt đấu giá của sản phẩm đó từ bảng HistoryAuctions
                // Kết quả sẽ có được 1 bảng gồm 2 cột ID sản phẩm và số lượt đấu giá của sản phẩm
                var history = (from h in db.HistoryAuctions
                               join p in db.Products on h.ProductID equals p.ID
                               where p.Status==1
                               group h by h.ProductID into his
                               select new { ProductId = his.Key, Num = his.Count() }).ToList();
                // Sắp xếp bảng vừa lấy được giảm dần và lấy 5 dòng đầu tiên
                history = history.OrderByDescending(h => h.Num).Take(8).ToList();

                // Truy vấn để lấy thông tin của của sản phẩm để truyền ra view
                var topAuction = (from h in history
                                  join p in db.Products on h.ProductId equals p.ID
                                  join c in db.Categories on p.CategoryID equals c.ID
                                  join u in db.Users on p.Winner equals u.ID
                                  join photo in db.Photos on p.ID equals photo.ProductID
                                  where p.Status == 1
                                  select new ProductHomeViewModel
                                  {
                                      ID = p.ID,
                                      Name = p.Name.Trim(),
                                      Winner = u.Name.Trim(),
                                      Price = p.PriceAuction,
                                      DateEnd = p.DateEnd,
                                      DateStart = p.DateStart,
                                      LinkImage = photo.Name,
                                      ImageUser = u.Photo,
                                      BuyItNow = p.BuyNowPice,
                                      CateID=c.ID,
                                      CateName=c.Name,
                                      WinnerID=u.ID,
                                      VendorID=p.VendorID
                                  }).ToList();
                // vì khi join với bảng photo thì 1 sản phẩm có thể có nhiều hình
                // mình chỉ cần lấy 1 hình đầu tiên làm hình đại diện
                // nên cần distinct theo ID sản phẩm
                topAuction = topAuction.DistinctBy(p => p.ID).ToList();
                return topAuction;
            }
        }

        /// <summary>
        /// GET: Home/Index
        /// </summary>
        /// <returns>Truyền ra view Top 5 sản phẩm giá cao nhất, nhiều lượt ra giá nhất, gần kết thúc </returns>
        public ActionResult Index()
        {
            using (var db = new WebDauGiaEntities())
            {
                // Kiểm tra sản phẩm hết hạn
                AccountController.CheckTimeOut(db);
                // Truy vấn lấy thông tin tất cả sản phẩm
                var list = (from p in db.Products
                            join photo in db.Photos on p.ID equals photo.ProductID
                            join c in db.Categories on p.CategoryID equals c.ID
                            join u in db.Users on p.Winner equals u.ID
                            where p.Status == 1
                            select new ProductHomeViewModel
                            {
                                ID = p.ID,
                                Name = p.Name.Trim(),
                                Winner = u.Name.Trim(),
                                Price = p.PriceAuction,
                                DateEnd = p.DateEnd,
                                DateStart = p.DateStart,
                                LinkImage = photo.Name,
                                ImageUser = u.Photo,
                                BuyItNow = p.BuyNowPice,
                                CateID = c.ID,
                                CateName = c.Name,
                                WinnerID=u.ID,
                                Status=p.Status,
                                VendorID=p.VendorID
                            }).ToList();
                // vì khi join với bảng photo thì 1 sản phẩm có thể có nhiều hình
                // mình chỉ cần lấy 1 hình đầu tiên làm hình đại diện
                // nên cần distinct theo ID sản phẩm
                list = list.DistinctBy(p => p.ID).ToList();

                // Sắp xếp giảm dần theo giá và lấy 8 dòng đầu tiên
                ViewBag.TopPrice = list.OrderByDescending(p => p.Price).Take(8).ToList();

                // Sắp xếp giảm theo thời gian theo giá và lấy 8 dòng đầu tiên
                ViewBag.TopDateEnd = list.OrderBy(p => p.DateEnd).Take(8).ToList();

                // Lấy 5 sản phẩm có số lượt ra giá nhiều nhất
                ViewBag.TopAuction = GetTopAuction();
                return View();
            }
        }

        /// <summary>
        /// Lấy danh sách tên tất cả các sản phẩm để autocomplete
        /// </summary>
        /// <param name="name">từ khoá tìm kiếm</param>
        /// <returns></returns>
        public JsonResult Autocomplete(string name)
        {
            WebDauGiaEntities ctx = new WebDauGiaEntities();
            List<string> pro;
            pro = ctx.Products.Where(x => x.Name.StartsWith(name))
                 .Select(e => e.Name.Trim()).Distinct().ToList();
            return Json(pro, JsonRequestBehavior.AllowGet);

        }
        
    }
}