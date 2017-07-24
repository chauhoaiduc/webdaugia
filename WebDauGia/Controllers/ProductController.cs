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
    public class ProductController : Controller
    {
        /// <summary>
        /// Kiểm tra sản phẩm có phải là sp user yêu thích hay không?
        /// trong trang chi tiết sản phẩm
        /// </summary>
        /// <param name="productID">id sản phẩm</param>
        /// <returns>true: nếu đúng; false: ngược lại
        /// và truyền về _ButtonFavouritesPartialView</returns>
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
                return PartialView("_ButtonFavouritesPartialView", liked);
            }

        }

        /// <summary>
        /// Chỉnh sửa thông tin sản phẩm chỉ cho phép thêm mổ tả sản phẩm
        /// </summary>
        /// <param name="model"> chứa id sản phẩm và mô tả cần thêm</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(EditProductViewModel model)
        {
            
            using (var db = new WebDauGiaEntities())
            {
                var pro = db.Products.Find(model.ProductID);
                pro.Descrition = pro.Descrition +"<br>"+ model.Des;
                db.SaveChanges();
                ViewBag.MsgSuccess = "Chỉnh sửa thông tin sản phẩm thành công !";
                var list = (from p in db.Products
                            join v in db.Users on p.VendorID equals v.ID
                            join w in db.Users on p.Winner equals w.ID
                            where p.ID == model.ProductID
                            select new ProductDetailViewModel
                            {
                                ID = p.ID,
                                Name = p.Name,
                                VendorID = v.ID,
                                VendorName = v.Name.Trim(),
                                VendorImage = v.Photo,
                                Price = p.Price,
                                Description = p.Descrition,
                                DateStart = p.DateStart,
                                DateEnd = p.DateEnd,
                                PriceAuction = p.PriceAuction,
                                WinnerID = w.ID,
                                WinnerName = w.Name.Trim(),
                                StepPrice = p.StepPrice,
                                BuyNowPice = p.BuyNowPice,
                                Status = p.Status,
                                FeedbackVendor = (int)(((double)(v.Positive + v.Negative + 1) / (v.Positive + 1)) * 100),
                                FeedbackWinner = (int)(((double)(w.Positive + w.Negative + 1) / (w.Positive + 1)) * 100),
                            }).FirstOrDefault();
                var listUser = (from h in db.HistoryAuctions
                                join u in db.Users on h.UserID equals u.ID
                                where h.ProductID == model.ProductID && h.Denied == 0
                                select new HistoryAuctionProductVM
                                {
                                    UserID = h.UserID,
                                    UserName = u.Name,
                                    DateAction = h.DateAction
                                }).ToList();
                ViewBag.History = listUser.ToList();
                return View(list);
            }

        }
        /// <summary>
        /// Hiển thị thông tin chi tiết sản phẩm đã đăng
        /// </summary>
        /// <param name="productID">id sản phẩm</param>
        /// <param name="index">vị trí trang danh sách người đấu giá</param>
        /// <param name="message">thông báo nhận từ 1 hàm khác</param>
        /// <returns></returns>
        [CheckLogin]
        public ActionResult Edit(int? productID,int? index,string message)
        {
            ViewBag.MsgSuccess = message;
            if (!productID.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }
            if (!index.HasValue)
            {
                index=1;
            }
            //lấy id user đang đăng nhập
            var userID = CurrentContext.GetUser().ID;
            using (var db = new WebDauGiaEntities())
            {
                // lấy thông tin sản phẩm có id = productID và có VendorID = userID
                var pro = (from p in db.Products
                           join v in db.Users on p.VendorID equals v.ID
                           join w in db.Users on p.Winner equals w.ID
                           where p.ID == productID && p.VendorID==userID
                           select new ProductDetailViewModel
                           {
                               ID = p.ID,
                               Name = p.Name,
                               VendorID = v.ID,
                               VendorName = v.Name.Trim(),
                               VendorImage = v.Photo,
                               Price = p.Price,
                               Description = p.Descrition,
                               DateStart = p.DateStart,
                               DateEnd = p.DateEnd,
                               PriceAuction = p.PriceAuction,
                               WinnerID = w.ID,
                               WinnerName = w.Name.Trim(),
                               StepPrice = p.StepPrice,
                               BuyNowPice = p.BuyNowPice,
                               Status = p.Status,
                               FeedbackVendor = (int)(((double)(v.Positive + v.Negative + 1) / (v.Positive + 1)) * 100),
                               FeedbackWinner = (int)(((double)(w.Positive + w.Negative + 1) / (w.Positive + 1)) * 100),
                           }).FirstOrDefault();
                // Lấy danh sách user tham gia đấu giá sản phẩm
                var listUser = (from h in db.HistoryAuctions
                                join u in db.Users on h.UserID equals u.ID
                                where h.ProductID == productID && h.Denied == 0
                                select new HistoryAuctionProductVM
                                {
                                    UserID=h.UserID,
                                    UserName=u.Name,
                                    DateAction=h.DateAction
                                }).ToList();
                // phân trang danh sách user vừa tìm được
                int n = listUser.Count();
                int take = 6;

                ViewBag.Pages = Math.Ceiling((double)n / take);
                ViewBag.Previous = index - 1 < 1 ? 1 : index - 1;
                ViewBag.Next = index + 1 > ViewBag.Pages ? ViewBag.Pages : index + 1;
                ViewBag.History = listUser.OrderBy(h => h.DateAction).Skip((int)(index - 1) * take).Take(take).ToList();
                return View(pro);
            }
        }

        /// <summary>
        /// Lấy danh sách hình ảnh của sản phẩm
        /// </summary>
        /// <param name="productID">ID sản phẩm</param>
        /// <returns>Trả về partial view PostImagePartialView</returns>
        public ActionResult PostImage(int? productID)
        {
            using (var db = new WebDauGiaEntities())
            {
                var list = db.Photos.Where(p => p.ProductID == productID).ToList();
                return PartialView("_PostImagePartialView", list);
            }
        }

        /// <summary>
        /// GET: Product/Detail
        /// Trả về thông tin chi tiết sản phẩm
        /// </summary>
        /// <param name="productID">id sản phẩm</param>
        /// <param name="message">dùng để nhận thông báo trả về view detail từ 1 hàm khác</param>
        /// <returns>trả về view Detail hoặc view edit nếu người dùng hiện tại là người đăng sản phẩm</returns>
        public ActionResult Detail(int? productID, string msgerror,string msgsuccess)
        {
            // khi productID == null thì chuyển về trang chủ
            if (productID == null)
            {
                return RedirectToAction("Index", "Home");
            }
 
            //Dùng để nhận thông báo từ 1 hàm khác và xuất thông báo ra view
            ViewBag.MsgError = msgerror;
            ViewBag.MsgSuccess = msgsuccess;

            using (var db = new WebDauGiaEntities())
            {
                // Lấy id của người dùng hiện tại nếu người dùng đã đăng nhập
                // nếu người dùng chưa đăng nhập thì id = 0
                var uid = 0;

                if (CurrentContext.IsLogged())
                {
                    uid = CurrentContext.GetUser().ID;
                }

                // Kiểm tra sản phẩm hiện tại có phải là sản phẩm yêu thích của người dùng hay không
                // Nếu là có thì biến watching sẽ là true ngược lại là false
                // Nếu người dùng chưa đăng nhập 
                // hoặc sản phẩm hiện tại không phải là sản phẩm yêu thích của người dùng
                // thì danh sách trả về sẽ là danh sách rỗng
                var watch = db.WatchLists.Where(w => w.UserID == uid && w.ProductID == productID).ToList();
                bool watching = false;

                if (watch.Count != 0)
                {
                    watching = true;
                }

                // truy vấn lấy thông tin chi tiết của sản phẩm
                var list = (from p in db.Products
                            join v in db.Users on p.VendorID equals v.ID
                            join w in db.Users on p.Winner equals w.ID
                            where p.ID == productID
                            select new ProductDetailViewModel
                            {
                                ID = p.ID,
                                Name = p.Name.Trim(),
                                VendorID = v.ID,
                                VendorName = v.Name.Trim(),
                                VendorImage=v.Photo,
                                Price = p.Price,
                                Description = p.Descrition,
                                DateStart = p.DateStart,
                                DateEnd = p.DateEnd,
                                PriceAuction = p.PriceAuction,
                                WinnerID = w.ID,
                                WinnerName = w.Name.Trim(),
                                StepPrice = p.StepPrice,
                                BuyNowPice = p.BuyNowPice,
                                Watching = watching,
                                Status = p.Status,
                                FeedbackVendor = (int)(((double)(v.Positive + v.Negative + 1) / (v.Positive + 1)) * 100),
                                FeedbackWinner = (int)(((double)(w.Positive + w.Negative + 1) / (w.Positive + 1)) * 100),
                            }).FirstOrDefault();

                // Kiểm tra nếu người dùng đã đăng nhập
                if (uid != 0)
                {
                    // Kiểm tra xem nếu người dùng đã đăng nhập 
                    // thì người dùng có đang bị cấm đấu giá trên sản phẩm hiện tại hay không
                    ViewBag.DeniedProduct = db.DeniedProducts.Where(d => d.UserID == uid && d.ProductID == productID).FirstOrDefault();
                    // Kiểm tra xem nếu người dùng đã đăng nhập 
                    // thì người dùng có đang bị cấm đấu giá trên các sản phẩm của người đăng hay không
                    ViewBag.DeniedVendor = db.DeniedVendors.Where(d => d.UserID == uid && d.VendorID == list.VendorID).FirstOrDefault();

                    // Kiểm tra nếu sản phẩm hiện tại là sản phẩm do người dùng hiện tại đăng
                    // thì sẽ trả về trang chỉnh sửa thông tin sản phẩm
                    if (uid == list.VendorID && list.Status == 1)
                    {
                        return RedirectToAction("Edit","Product",new { productID= productID ,message= msgsuccess});
                    }
                }

                return View(list);
            }
        }

        /// <summary>
        /// GET: Product/Search tìm kiếm theo tên sản phẩm và tên thể loại
        /// </summary>
        /// <param name="search">từ khóa tìm kiếm</param>
        /// <param name="memid">memid dùng cho sắp xếp mặc định thì sắp xếp theo ID ; 1 thì sắp xếp giá tăng dần ; 2 thì sắp xếp theo thời gian kết thúc giảm dần</param>
        /// <param name="index">số trang hiện tại</param>
        /// <returns>Danh sách sản phẩm tìm kiếm theo từ khóa search ra View</returns>
        public ActionResult Search(string search, int? memid, int? index)
        {
            //Truyền từ khóa search và MemID ra View để dùng phân trang sắp xếp
            if (search == null)
            {
                search = " ";
            }
            ViewBag.Search = search;
            ViewBag.MemID = memid;
            
            // Nếu trang hiện tại là null thì gán là 1
            if (!index.HasValue)
            {
                index = 1;
            }
            else
            {

            }
            using (var db = new WebDauGiaEntities())
            {
                AccountController.CheckTimeOut(db);
                // Tìm kiếm danh sách các sản phẩm theo từ khóa
                // vì khi join với bảng photo thì 1 sản phẩm có thể có nhiều hình
                // mình chỉ cần lấy 1 hình đầu tiên làm hình đại diện
                // nên cần distinct theo ID sản phẩm
                var list = (from p in db.Products
                            join photo in db.Photos on p.ID equals photo.ProductID
                            join c in db.Categories on p.CategoryID equals c.ID
                            join u in db.Users on p.Winner equals u.ID
                            where (p.Name.Contains(search) || c.Name.Contains(search)) && p.Status == 1
                            select new ProductHomeViewModel
                            {
                                ID = p.ID,
                                Name = p.Name,
                                Winner = u.Name.Trim(),
                                Price = p.PriceAuction,
                                DateEnd = p.DateEnd,
                                DateStart = p.DateStart,
                                LinkImage = photo.Name,
                                ImageUser = u.Photo,
                                BuyItNow = p.BuyNowPice,
                                CateID = c.ID,
                                CateName = c.Name,
                                VendorID=p.VendorID
                            }).ToList();
                list = list.DistinctBy(p => p.ID).ToList();
                // lấy số lượng sản phẩm đã tìm kiếm
                int n = ViewBag.Count = list.Count;
                // Số lượng sản phẩm xuất hiện trong 1 trang
                int take = 6;

                // Tính số lượng trang = số lượng sản phẩm / số sản phẩm trong 1 trang
                // Làm tròn lên ví dụ 5.3 thành 6 và gán cho ViewBag để truyền ra view
                ViewBag.Pages = Math.Ceiling((double)n / take);
                // Lấy giá trị trang trước nếu nhỏ hơn 1 thì gán là 1
                // ngược lại thì lấy index - 1
                ViewBag.Previous = index - 1 < 1 ? 1 : index - 1;
                // Lấy giá trị trang sau nếu lớn hơn tổng số trang thì gán bằng tổng số trang
                // ngược lại thì lấy index + 1
                ViewBag.Next = index + 1 > ViewBag.Pages ? ViewBag.Pages : index + 1;

                // nếu menid có giá trị thì sắp xếp theo giá hoặc theo thời gian
                // ngược lại sắp xếp theo id
                if (memid.HasValue)
                {
                    // Dùng cho sắp xếp nếu memid = 1 thì sắp theo giá tăng dần
                    // memid = 2 thì sắp theo giá tăng dần
                    // memid = 3 thì sắp theo thời gian kết thúc giảm dần
                    // memid = 4 thì sắp theo thời gian kết thúc tăng dần
                    if (memid == 1)
                    {
                        list = list.OrderBy(p => p.Price).Skip((int)(index - 1) * take).Take(take).ToList();
                    }
                    else if (memid == 2)
                    {
                        list = list.OrderByDescending(p => p.Price).Skip((int)(index - 1) * take).Take(take).ToList();
                    }
                    else if (memid == 3)
                    {
                        list = list.OrderBy(p => p.DateEnd).Skip((int)(index - 1) * take).Take(take).ToList();
                    }
                    else
                    {
                        list = list.OrderByDescending(p => p.DateEnd).Skip((int)(index - 1) * take).Take(take).ToList();
                    }
                }
                else
                {
                    // Có phân trang ví dụ trang hiện tại là 1 thì chỉ lấy từ sản phẩm thứ 1->5
                    // nếu trang hiện tại là 2 thì chỉ lấy từ sản phẩm thứ 6->10
                    list = list.OrderBy(p => p.ID).Skip((int)(index - 1) * take).Take(take).ToList();
                }
                
                return View(list);
            }
        }

        /// <summary>
        /// GET: Product/ByCategory
        /// </summary>
        /// <param name="id">ID của category</param>
        /// <param name="memid">memid dùng cho sắp xếp mặc định thì sắp xếp theo ID ; 1 thì sắp xếp giá tăng dần ; 2 thì sắp xếp theo thời gian kết thúc giảm dần </param>
        /// <param name="index">số trang hiện tại</param>
        /// <returns>Danh sách sản phẩm theo thể loại ra View</returns>
        /// 
        public ActionResult ByCategory(int? catid, int? memid, int? index)
        {
            if (catid.HasValue)
            {
                // Nếu trang hiện tại là null thì gán là 1
                if (!index.HasValue)
                {
                    index = 1;
                }
                else
                {

                }

                using (var db = new WebDauGiaEntities())
                {
                    AccountController.CheckTimeOut(db);
                    // Lấy danh sách các sản phẩm có categoryID = id
                    // vì khi join với bảng photo thì 1 sản phẩm có thể có nhiều hình
                    // mình chỉ cần lấy 1 hình đầu tiên làm hình đại diện
                    // nên cần distinct theo ID sản phẩm
                    var list = (from p in db.Products
                                join photo in db.Photos on p.ID equals photo.ProductID
                                join u in db.Users on p.Winner equals u.ID
                                join c in db.Categories on p.CategoryID equals c.ID
                                where p.CategoryID == catid && p.Status == 1
                                select new ProductHomeViewModel
                                {
                                    ID = p.ID,
                                    Name = p.Name,
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
                                    VendorID=p.VendorID
                                }).DistinctBy(p => p.ID).ToList();

                    // lấy số lượng sản phẩm thuộc thể loai có ID = id
                    int n = ViewBag.Count= list.Count;
                    // Số lượng sản phẩm xuất hiện trong 1 trang
                    int take = 6;

                    // Tính số lượng trang = số lượng sản phẩm / số sản phẩm trong 1 trang
                    // Làm tròn lên ví dụ 5.3 thành 6 và gán cho ViewBag để truyền ra view
                    ViewBag.Pages = Math.Ceiling((double)n / take);
                    // Lấy giá trị trang trước nếu nhỏ hơn 1 thì gán là 1
                    // ngược lại thì lấy index - 1
                    ViewBag.Previous = index - 1 < 1 ? 1 : index - 1;
                    // Lấy giá trị trang sau nếu lớn hơn tổng số trang thì gán bằng tổng số trang
                    // ngược lại thì lấy index + 1
                    ViewBag.Next = index + 1 > ViewBag.Pages ? ViewBag.Pages : index + 1;

                    

                    // nếu menid có giá trị thì sắp xếp theo giá hoặc theo thời gian
                    // ngược lại sắp xếp theo id
                    if (memid.HasValue)
                    {
                        // Dùng cho sắp xếp nếu memid = 1 thì sắp theo giá tăng dần
                        // memid = 2 thì sắp theo giá tăng dần
                        // memid = 3 thì sắp theo thời gian kết thúc giảm dần
                        // memid = 4 thì sắp theo thời gian kết thúc tăng dần
                        if (memid == 1)
                        {
                            list = list.OrderBy(p => p.Price).Skip((int)(index - 1) * take).Take(take).ToList();
                        }
                        else if (memid == 2)
                        {
                            list = list.OrderByDescending(p => p.Price).Skip((int)(index - 1) * take).Take(take).ToList();
                        }
                        else if (memid == 3)
                        {
                            list = list.OrderBy(p => p.DateEnd).Skip((int)(index - 1) * take).Take(take).ToList();
                        }
                        else
                        {
                            list = list.OrderByDescending(p => p.DateEnd).Skip((int)(index - 1) * take).Take(take).ToList();
                        }
                    }
                    else
                    {
                        // Có phân trang ví dụ trang hiện tại là 1 thì chỉ lấy từ sản phẩm thứ 1->5
                        // nếu trang hiện tại là 2 thì chỉ lấy từ sản phẩm thứ 6->10
                        list = list.OrderBy(p => p.ID).Skip((int)(index - 1) * take).Take(take).ToList();
                    }                   
                    //Truyền categoryID và MemID ra View để dùng phân trang sắp xếp
                    ViewBag.CateID = catid;
                    ViewBag.MemID = memid;
                    return View(list);
                }
            }
            else
            {
                return RedirectToAction("Index","Home");
            }

        }
    }
}