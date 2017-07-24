using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using WebDauGia.Models;
using WebDauGia.Helper;
using BotDetect.Web.Mvc;
using WebDauGia.Filters;
using System.Net.Mail;
using System.Text;
using System.IO;
using System.Reflection;
using Microsoft.Ajax.Utilities;
using System.Data.Entity;
using System.Collections.Generic;

namespace WebDauGia.Controllers
{
    public class AccountController : Controller
    {
        /// <summary>
        /// Cấm quyền user đấu giá trên tất cả các sản phẩm mà mình đăng
        /// </summary>
        /// <param name="vendorID">id người đăng</param>
        /// <param name="userID">id user bị cấm</param>
        /// <param name="proID">id sản phẩm dùng để trả về trang edit</param>
        /// <returns></returns>
        [CheckLogin]
        public ActionResult DeniedVendor(int vendorID, int userID,int proID)
        {
            string notify;

            using (var db = new WebDauGiaEntities())
            {
                // Lấy danh sách sản phẩm của người đăng
                var listpro = db.Products.Where(p => p.VendorID == vendorID).ToList();
                // duyệt qua từng sản phẩm
                foreach (var item in listpro)
                {
                    // thêm id sản phẩm và id user vào bảng DeniedProduct
                    var denied = new DeniedProduct
                    {
                        ProductID = item.ID,
                        UserID = userID
                    };
                    db.DeniedProducts.Add(denied);
                    
                    // Vào lịch sử đấu giá của sản phẩm xem có user có đang đấu giá hay không ?
                    // nếu có thì gán Denied =1
                    var listuser = db.HistoryAuctions.Where(h => h.UserID == userID && h.ProductID == item.ID).ToList();

                    foreach (var temp in listuser)
                    {
                        temp.Denied = 1;
                    }
                    db.SaveChanges();

                    //Lấy thông tin của sản phẩm 
                    var pro = db.Products.Find(item.ID);
                    //Nếu user bị cấm là người giữ giá cao nhất thì chuyển cho người đấu giá thấp hơn
                    if (userID == pro.Winner)
                    {
                        // hoàn tiền lại cho user 
                        Refund(item.ID);
                        // vào lịch sử đấu giá của sản phẩm lấy nhứng user không bị cấm quyền
                        var history = db.HistoryAuctions.Where(h => h.ProductID == item.ID && h.Denied != 1).ToList();
                        // sắp xếp giảm dần theo giá
                        history = history.OrderByDescending(h => h.Price).ToList();
                        // bỏ những record có trùng userID
                        history = history.DistinctBy(h => h.UserID).ToList();
                        // Tìm user có giá cao tiếp theo
                        // với điều kiện user phải có đủ credit
                        // nếu không có ai thoải điều kiện sản phẩm sẽ quay về giá khởi điểm
                        int flag = 0;
                        if (history.Count > 1)
                        {
                            for (int i = 0; i < history.Count - 1; i++)
                            {
                                // lấy thông tin user theo history[i].UserID
                                var user = db.Users.Find(history[i].UserID);
                                // nếu user đủ credit thì user trở thành người gửi giá
                                // ngược lại sẽ xóa user ra khỏi lịch sử đấu giá
                                if (user.Credits > history[i].Price)
                                {
                                    flag = 1;
                                    // nếu không có ai đấu giá thấp hơn user 
                                    // thì giá hiển thị là giá khởi điểm
                                    // ngược lại giá hiển thị là giá đấu của người đấu giá thấp hơn
                                    if (i + 1 == history.Count)
                                    {
                                        pro.PriceAuction = pro.Price;
                                    }
                                    else
                                    {
                                        pro.PriceAuction = history[i + 1].Price;
                                    }
                                    // gán giá đấu cao nhất của sản phẩm = history[i].Price
                                    // người gửi giá = history[i].UserID
                                    pro.TopPrice = history[i].Price;
                                    pro.Winner = history[i].UserID;
                                    break;
                                }
                                else
                                {
                                    db.HistoryAuctions.Attach(history[i]);
                                    db.HistoryAuctions.Remove(history[i]);
                                }
                            }
                        }
                        else if(history.Count != 0)
                        {
                            var user = db.Users.Find(history.ElementAt(0).UserID);
                            if (user.Credits > history.ElementAt(0).Price)
                            {
                                flag = 1;
                                pro.TopPrice = history.ElementAt(0).Price;
                                pro.Winner = history.ElementAt(0).UserID;
                                pro.PriceAuction = pro.Price;
                            }
                            else
                            {
                                db.HistoryAuctions.Attach(history.ElementAt(0));
                                db.HistoryAuctions.Remove(history.ElementAt(0));
                            }
                        }
                        if (flag == 0)
                        {
                            pro.PriceAuction = pro.TopPrice = pro.Price;
                            pro.Winner = pro.VendorID;
                        }
                    }
                    db.SaveChanges();
                }
            }
            notify = "Cấm quyền user thành công !";
            return RedirectToAction("Detail", "Product", new { productID = proID, msgsuccess = notify });
        }

        /// <summary>
        /// Cấm quyền user đấu giá trên sản phẩm hiện tịa
        /// </summary>
        /// <param name="productID">id sản phẩm hiện tại</param>
        /// <param name="userID">id user bị cấm</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult DeniedProduct(int productID, int userID)
        {
            string notify;

            using (var db = new WebDauGiaEntities())
            {
                // thêm id sản phẩm và id user vào bảng DeniedProduct
                var denied = new DeniedProduct
                {
                    ProductID = productID,
                    UserID = userID
                };
                db.DeniedProducts.Add(denied);

                // Vào lịch sử đấu giá của sản phẩm xem có user có đang đấu giá hay không ?
                // nếu có thì gán Denied =1
                var listuser = db.HistoryAuctions.Where(h => h.UserID == userID && h.ProductID == productID).ToList();

                foreach (var temp in listuser)
                {
                    temp.Denied = 1;
                }
                db.SaveChanges();

                //Lấy thông tin của sản phẩm 
                var pro = db.Products.Find(productID);
                //Nếu user bị cấm là người giữ giá cao nhất thì chuyển cho người đấu giá thấp hơn
                if (userID == pro.Winner)
                {
                    //hoàn tiền lại cho user
                    Refund(productID);
                    // vào lịch sử đấu giá của sản phẩm lấy nhứng user không bị cấm quyền
                    var history = db.HistoryAuctions.Where(h => h.ProductID == productID && h.Denied != 1).ToList();
                    // sắp xếp giảm dần theo giá
                    history = history.OrderByDescending(h => h.Price).ToList();
                    // bỏ những record có trùng userID
                    history = history.DistinctBy(h => h.UserID).ToList();
                    // Tìm user có giá cao tiếp theo
                    // với điều kiện user phải có đủ credit
                    // nếu không có ai thoải điều kiện sản phẩm sẽ quay về giá khởi điểm
                    int flag = 0;
                    if (history.Count > 1)
                    {
                        for (int i = 0; i < history.Count - 1; i++)
                        {
                            // lấy thông tin user theo history[i].UserID
                            var user = db.Users.Find(history[i].UserID);
                            // nếu user đủ credit thì user trở thành người gửi giá
                            // ngược lại sẽ xóa user ra khỏi lịch sử đấu giá
                            if (user.Credits > history[i].Price)
                            {
                                flag = 1;
                                // nếu không có ai đấu giá thấp hơn user 
                                // thì giá hiển thị là giá khởi điểm
                                // ngược lại giá hiển thị là giá đấu của người đấu giá thấp hơn
                                if (i + 1 == history.Count)
                                {
                                    pro.PriceAuction = pro.Price;
                                }
                                else
                                {
                                    pro.PriceAuction = history[i + 1].Price;
                                }
                                // gán giá đấu cao nhất của sản phẩm = history[i].Price
                                // người gửi giá = history[i].UserID
                                pro.TopPrice = history[i].Price;
                                pro.Winner = history[i].UserID;
                                break;
                            }
                            else
                            {
                                db.HistoryAuctions.Attach(history[i]);
                                db.HistoryAuctions.Remove(history[i]);
                            }
                        }
                    }
                    else
                    {
                        var user = db.Users.Find(history.ElementAt(0).UserID);
                        if (user.Credits > history.ElementAt(0).Price)
                        {
                            flag = 1;
                            pro.TopPrice = history.ElementAt(0).Price;
                            pro.Winner = history.ElementAt(0).UserID;
                            pro.PriceAuction = pro.Price;
                        }
                        else
                        {
                            db.HistoryAuctions.Attach(history.ElementAt(0));
                            db.HistoryAuctions.Remove(history.ElementAt(0));
                        }
                    }
                    if (flag == 0)
                    {
                        pro.PriceAuction = pro.TopPrice = pro.Price;
                        pro.Winner = pro.VendorID;
                    }
                }
                db.SaveChanges();
                DecreaseCredits(productID,0);
                notify = "Cấm quyền user thành công !";
            }

            return RedirectToAction("Detail", "Product", new { productID = productID, msgsuccess = notify });
        }
        /// <summary>
        /// Hiện thị trang mua chức năng đấu giá
        /// </summary>
        /// <returns></returns>
        public ActionResult BecomeSeller()
        {
            return View();
            
        }
        /// <summary>
        /// User đăng ký trở thành người đăng sản phẩm
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Seller()
        {
            var userID = CurrentContext.GetUser().ID;
            using (var db = new WebDauGiaEntities())
            {
                var user = db.Users.Find(userID);
                user.SaleStatus = 1;
                user.Credits -= 100;
                db.SaveChanges();
                Session["user"] = user;
                ViewBag.MsgSuccess = "Bạn đã mua chức năng thành vui lòng đợi admin duyệt.";
                return View("BecomeSeller");
            }
        }
        /// <summary>
        /// Kiểm tra những sản phẩm đã hết hạn
        /// </summary>
        /// <param name="db"></param>
        public static void CheckTimeOut(WebDauGiaEntities db)
        {
            // lấy danh sách tất cả sản phẩm
            var list = db.Products.Where(p => p.Status == 1);
            foreach (var item in list)
            {
                // Nếu đã hết hạn thì bật status = 2
                if (item.DateEnd < DateTime.Now)
                {
                    item.Status = 2;
                    // trừ tiền người gửi giá cao nhất có gửi email thông báo
                    // tăng tiền cho người đăng sản phẩm có gửi email thông báo
                    if (item.Winner != item.VendorID)
                    {
                        DecreaseCredits(item.ID, 1);
                        IncreaseCredits(item.ID);
                    }
                    else
                    {
                        // nếu không có ai mua thông báo cho người đăng
                        GuiMailNguoiBan(item.ID, 0);
                    }
                }
            }
            db.SaveChanges();
        }
        /// <summary>
        /// Sản phẩm hết thời gian Khi đang ở trang chi tiết sản phẩm
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Timeout(int ID)
        {
            using (var db = new WebDauGiaEntities())
            {
                var pro = db.Products.Find(ID);
                pro.Status = 2;
                db.SaveChanges();
                if (pro.Winner != pro.VendorID)
                {
                    DecreaseCredits(ID, 1);
                    IncreaseCredits(ID);
                }
                else
                {
                    GuiMailNguoiBan(ID, 0);
                }
                return RedirectToAction("Index", "Home");
            }
                
        }
        /// <summary>
        /// Mua sản phẩm ngay không cần đấu giá
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckLogin]
        public ActionResult BuyItNow(int productID)
        {
            using (var db = new WebDauGiaEntities())
            {
                // lấy thông tin sản phẩm và user đang đăng nhập
                var pro = db.Products.Find(productID);
                var uc = CurrentContext.GetUser();
                // nếu credits của user không đủ để mua sản phẩm sẽ thông báo
                if (uc.Credits < pro.BuyNowPice)
                {
                    string notify = "Xin lỗi bạn không đủ tiền mua sản phẩm này";
                    return RedirectToAction("Detail", "Product", new { productID = productID, msgerror = notify });
                }
                else
                {
                    // ngược lại hoàn tiền cho người gửi giá cao nhất
                    Refund(productID);
                    // gán giá cao nhất của sản phẩm và giá đấu = giá mua ngay
                    pro.TopPrice = pro.BuyNowPice;
                    pro.PriceAuction = pro.BuyNowPice;
                    pro.Winner = CurrentContext.GetUser().ID;
                    pro.Status = 2;
                    db.SaveChanges();
                    // trừ tiền user và tăng tiền cho người bán có gửi email thông báo
                    DecreaseCredits(productID, 1);
                    IncreaseCredits(productID);
                    var vendor = (from u in db.Users where u.ID == pro.VendorID select u.Name).ToString();
                    var list = new BuyItNowSuccessViewModel
                    {
                        ProductID = pro.ID,
                        ProductName = pro.Name,
                        Descrition = pro.Descrition,
                        DateStart = pro.DateStart,
                        VendorName = vendor,
                        Price = pro.Price
                    };

                    return View("BuyItNowSuccessViewModel", list);
                }

            }
        }
        /// <summary>
        /// Hiển thị Danh sách sản phẩm đăng đã có người mua có phân trang
        /// </summary>
        /// <param name="index"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        [CheckLogin]
        public ActionResult SaleProduct(int? index, string message)
        {
            ViewBag.Message = message;
            if (!index.HasValue)
            {
                index = 1;
            }
            else
            {

            }
            using (var db = new WebDauGiaEntities())
            {
                var userID = CurrentContext.GetUser().ID;
                var pro = (from p in db.Products
                           join photo in db.Photos on p.ID equals photo.ProductID
                           where p.Status == 2 && p.VendorID == userID && p.Winner != userID
                           select new WinProductViewModel
                           {
                               ID = p.ID,
                               Name = p.Name,
                               Des = p.Descrition,
                               Image = photo.Name,
                               DateEnd = p.DateEnd,
                               PriceAuction = p.PriceAuction,
                               WinnerID = p.Winner
                           }
                           ).ToList();
                pro = pro.DistinctBy(p => p.ID).ToList();

                int n = pro.Count();
                int take = 6;
                ViewBag.Pages = Math.Ceiling((double)n / take);
                ViewBag.Previous = index - 1 < 1 ? 1 : index - 1;
                ViewBag.Next = index + 1 > ViewBag.Pages ? ViewBag.Pages : index + 1;
                pro = pro.OrderBy(p => p.ID).Skip((int)(index - 1) * take).Take(take).ToList();

                return View(pro);
            }
        }
        /// <summary>
        /// Hiển thị trang Quản lý danh sách sản phẩm đã đăng chưa hết hạn
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        [CheckLogin]
        public ActionResult ManageItems(int? index)
        {
            if (!index.HasValue)
            {
                index = 1;
            }
            else
            {

            }
            using (var db = new WebDauGiaEntities())
            {
                var userID = CurrentContext.GetUser().ID;
                var list = (from p in db.Products
                            join photo in db.Photos on p.ID equals photo.ProductID
                            join c in db.Categories on p.CategoryID equals c.ID
                            join u in db.Users on p.Winner equals u.ID
                            where p.Status == 1 && p.VendorID == userID
                            select new ProductHomeViewModel
                            {
                                ID = p.ID,
                                Name = p.Name,
                                Winner = u.Name.Trim(),
                                WinnerID = u.ID,
                                Price = p.PriceAuction,
                                DateEnd = p.DateEnd,
                                DateStart = p.DateStart,
                                LinkImage = photo.Name,
                                ImageUser = u.Photo,
                                BuyItNow = p.BuyNowPice,
                                CateID = c.ID,
                                CateName = c.Name
                            }).ToList();
                list = list.DistinctBy(p => p.ID).ToList();

                int n = list.Count();
                int take = 6;

                ViewBag.Pages = Math.Ceiling((double)n / take);
                ViewBag.Previous = index - 1 < 1 ? 1 : index - 1;
                ViewBag.Next = index + 1 > ViewBag.Pages ? ViewBag.Pages : index + 1;

                list = list.OrderBy(p => p.ID).Skip((int)(index - 1) * take).Take(take).ToList();
                return View(list);
            }
        }
        /// <summary>
        /// Đăng sản phẩm
        /// </summary>
        /// <param name="pro"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckLogin]
        [ValidateInput(false)]
        public ActionResult UploadItem(UploadItemViewModel pro)
        {
            using (var db = new WebDauGiaEntities())
            {
                // khởi 1 selectList từ danh sách catgory
                var list = new SelectList(db.Categories.ToList(), "ID", "Name");

                var product = new Product();
                // thêm hình sản phẩm
                // flag dùng để kiểm tra user phải chọn ít nhất 1 hình sản phẩm
                int flag = 0;
                for (int i = 0; i < Request.Files.Count; i++)
                {
                    HttpPostedFileBase file = Request.Files[i];
                    if (file != null && file.ContentLength > 0)
                    {
                        flag = 1;
                        string[] fileExtensions = new string[] { ".jpg", ".png" };
                        // kiểm tra định dạng file là .jpg hoặc .png
                        if (fileExtensions.Contains(Path.GetExtension(file.FileName).ToLower()))
                        {
                            // lấy tên file
                            var fileName = Path.GetFileName(file.FileName);
                            // tạo đường dẫn và lưu hình ảnh
                            var path = Path.Combine(Server.MapPath($"~/Images/{pro.CategoryID}/"), fileName);
                            file.SaveAs(path);
                            // lưu hình ảnh vào cơ sở dữ liệu
                            var image = new Photo();
                            image.Name = $"~/Images/{pro.CategoryID}/{fileName}";
                            image.ProductID = product.ID;
                            db.Photos.Add(image);
                        }
                        else
                        {
                            ViewBag.Message = "Vui lòng chọn hình ảnh sản phẩm có định dạng jpg hoặc png !";
                            return View(list);
                        }
                    }
                }
                if (flag == 0)
                {
                    ViewBag.Message = "Vui lòng chọn ít nhất 1 hình ảnh sản phẩm !";
                    return View(list);
                }

                // Lưu sản phẩm vào cở sở dử liệu
                int userID = CurrentContext.GetUser().ID;
                // kiểm tra xem có mua chức năng tự động gia hạn không?
                // nếu có user sẽ bị trừ 10 credits
                if (Request.Form.GetValues("AutoExtend") != null)
                {
                    product.AutoExtend = true;
                    var user = db.Users.Find(userID);
                    if (user.Credits > 10)
                    {
                        user.Credits -= 10;
                        Session["user"] = user;
                    }
                    else
                    {
                        ViewBag.Message = "Credits của bạn không đủ để thêm tính năng tự động gia hạn !";
                        return View(list);
                    }
                }
                else
                {
                    product.AutoExtend = false;
                }

                product.Status = 0;
                product.DateStart = DateTime.Now;
                product.Name = pro.Name;
                product.CategoryID = pro.CategoryID;
                product.Price = product.PriceAuction = product.TopPrice = pro.Price;
                product.StepPrice = pro.StepPrice;
                product.Descrition = pro.Des;
                product.VendorID = product.Winner = userID;
                // Kiểm tra xem có giá mua ngay không ?
                if (Request.Form.GetValues("ShowBuyItNow") != null)
                {
                    product.BuyNowPice = pro.BuyNowPrice;
                }
                else
                {
                    product.BuyNowPice = 0;
                }

                db.Products.Add(product);
                db.SaveChanges();
                ViewBag.Success = "Đăng sản phẩm thành công !";

                return View(list);
            }
        }
        /// <summary>
        /// Hiển thị trang đăng sản phẩm
        /// </summary>
        /// <returns></returns>
        [CheckLogin]
        public ActionResult UploadItem()
        {
            var user = CurrentContext.GetUser();
            if (user.DateEndSale < DateTime.Now)
            {
                return RedirectToAction("Index", "Home");
            }
            using (var db = new WebDauGiaEntities())
            {
                var list = new SelectList(db.Categories.ToList(), "ID", "Name");
                return View(list);
            }
        }
        /// <summary>
        /// Đánh giá của người đăng dành cho người chiến thắng
        /// </summary>
        /// <param name="model">chứa id người chiến thắng ,
        /// comment, điểm đánh giá của người đăng dành cho người chiến thắng</param>
        /// <returns></returns>
        [CheckLogin]
        [HttpPost]
        public ActionResult CommentWinner(CommentUserViewModel model)
        {
            // lấy id người đăng
            int userID = CurrentContext.GetUser().ID;
            using (var db = new WebDauGiaEntities())
            {
                // Thêm thông tin comment vào csdl
                var cm = new CommentWinner
                {
                    ProductID = model.ProductID,
                    VendorID = userID,
                    WinnerID = model.UserID,
                    Comment = model.Comment,
                    Point = model.Point
                };
                db.CommentWinners.Add(cm);
                // lấy thông tin người chiến thắng
                var user = db.Users.Find(model.UserID);
                // nếu Point > 0 thì tăng điểm tích cực của người đấu giá thêm 1
                if (model.Point > 0)
                {
                    user.Positive += 1;
                }
                else
                {
                    user.Negative -= 1;
                }
                db.SaveChanges();
                string notify = "Đánh giá người bán thành công";
                return RedirectToAction("SaleProduct", "Account", new { message = notify });
            }
        }
        /// <summary>
        /// Đánh giá của người chiến thắng dành cho người đăng
        /// </summary>
        /// <param name="model">chứa id người đăng ,
        /// comment, điểm đánh giá của người chiến thắng dành cho người đăng</param>
        /// <returns></returns>
        [CheckLogin]
        [HttpPost]
        public ActionResult CommentVendor(CommentUserViewModel model)
        {
            // lấy id người chiến thắng
            int userID = CurrentContext.GetUser().ID;
            using (var db = new WebDauGiaEntities())
            {
                // Thêm thông tin comment vào csdl
                var cm = new CommentVendor
                {
                    ProductID = model.ProductID,
                    VendorID = model.UserID,
                    WinnerID = userID,
                    Comment = model.Comment,
                    Point = model.Point
                };
                db.CommentVendors.Add(cm);
                // lấy thông tin người đăng
                var user = db.Users.Find(model.UserID);
                // nếu Point > 0 thì tăng điểm tích cực của người đấu giá thêm 1
                if (model.Point > 0)
                {
                    user.Positive += 1;
                }
                else
                {
                    user.Negative -= 1;
                }
                db.SaveChanges();
                string notify = "Đánh giá người bán thành công";
                return RedirectToAction("WinProduct", "Account", new { message = notify });
            }
        }
        /// <summary>
        /// Hiển thị danh sách sản phẩm đã chiến thắng đấu giá có phân trang
        /// </summary>
        /// <param name="index">số trang hiện tại</param>
        /// <param name="message">Nhận thông báo từ 1 hàm khác</param>
        /// <returns></returns>
        [CheckLogin]
        public ActionResult WinProduct(int? index, string message)
        {
            if (!index.HasValue)
            {
                index = 1;
            }
            else
            {

            }
            ViewBag.Message = message;
            using (var db = new WebDauGiaEntities())
            {
                var userID = CurrentContext.GetUser().ID;
                var pro = (from p in db.Products
                           join photo in db.Photos on p.ID equals photo.ProductID
                           where p.Status == 2 && p.Winner == userID && p.VendorID != userID
                           select new WinProductViewModel
                           {
                               ID = p.ID,
                               Name = p.Name,
                               Des = p.Descrition,
                               Image = photo.Name,
                               DateEnd = p.DateEnd,
                               Price = p.TopPrice,
                               PriceAuction = p.PriceAuction,
                               VendorID = p.VendorID
                           }
                           ).ToList();
                pro = pro.DistinctBy(p => p.ID).ToList();

                int n = pro.Count();
                int take = 6;
                ViewBag.Pages = Math.Ceiling((double)n / take);
                ViewBag.Previous = index - 1 < 1 ? 1 : index - 1;
                ViewBag.Next = index + 1 > ViewBag.Pages ? ViewBag.Pages : index + 1;
                pro = pro.OrderBy(p => p.ID).Skip((int)(index - 1) * take).Take(take).ToList();

                return View(pro);
            }

        }
        /// <summary>
        /// Hiển thị danh sách sản phẩm đang tham giá đấu giá
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        [CheckLogin]
        public ActionResult JoiningAuction(int? index)
        {
            if (!index.HasValue)
            {
                index = 1;
            }
            else
            {

            }

            using (var db = new WebDauGiaEntities())
            {
                var uid = CurrentContext.GetUser().ID;
                var his = db.HistoryAuctions.Where(w => w.UserID == uid).ToList();

                int n = his.Count();
                int take = 6;

                ViewBag.Pages = Math.Ceiling((double)n / take);
                ViewBag.Previous = index - 1 < 1 ? 1 : index - 1;
                ViewBag.Next = index + 1 > ViewBag.Pages ? ViewBag.Pages : index + 1;

                var list = (from h in his
                            join p in db.Products on h.ProductID equals p.ID
                            join photo in db.Photos on p.ID equals photo.ProductID
                            join c in db.Categories on p.CategoryID equals c.ID
                            join u in db.Users on p.Winner equals u.ID
                            where p.Status == 1
                            select new ProductHomeViewModel
                            {
                                ID = p.ID,
                                Name = p.Name,
                                Winner = u.Name.Trim(),
                                WinnerID=u.ID,
                                Price = p.PriceAuction,
                                DateEnd = p.DateEnd,
                                DateStart = p.DateStart,
                                LinkImage = photo.Name,
                                ImageUser = u.Photo,
                                BuyItNow = p.BuyNowPice,
                                CateID = c.ID,
                                CateName = c.Name,
                                VendorID=p.VendorID
                            }).OrderBy(p => p.ID).Skip((int)(index - 1) * take).Take(take).ToList();
                list = list.DistinctBy(p => p.ID).ToList();
                return View(list);
            }

        }
        /// <summary>
        /// Hiển thị danh sách sản phẩm yêu thích
        /// </summary>
        /// <param name="search"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        [CheckLogin]
        public ActionResult WatchList(string search,int? index)
        {
            if (search == null)
            {
                search = " ";
            }
            ViewBag.Search = search.ToLower();
            if (!index.HasValue)
            {
                index = 1;
            }
            else
            {

            }

            using (var db = new WebDauGiaEntities())
            {
                var uid = CurrentContext.GetUser().ID;
                var watch = db.WatchLists.Where(w => w.UserID == uid).ToList();
                var list = (from w in watch
                            join p in db.Products on w.ProductID equals p.ID
                            join photo in db.Photos on p.ID equals photo.ProductID
                            join c in db.Categories on p.CategoryID equals c.ID
                            join u in db.Users on p.Winner equals u.ID
                            where (p.Name.ToLower().Contains(search) || c.Name.ToLower().Contains(search)) && p.Status == 1
                            select new ProductHomeViewModel
                            {
                                ID = p.ID,
                                Name = p.Name,
                                Winner = u.Name.Trim(),
                                WinnerID=u.ID,
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
                int n = list.Count();
                int take = 6;

                ViewBag.Pages = Math.Ceiling((double)n / take);
                ViewBag.Previous = index - 1 < 1 ? 1 : index - 1;
                ViewBag.Next = index + 1 > ViewBag.Pages ? ViewBag.Pages : index + 1;

                list=list.OrderBy(p => p.ID).Skip((int)(index - 1) * take).Take(take).ToList();
                
                return View(list);
            }

        }
        /// <summary>
        /// Chỉnh sửa thông tin cá nhân user
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Setting(SettingViewModel model)
        {
            using (var db = new WebDauGiaEntities())
            {
                var user = db.Users.Find(CurrentContext.GetUser().ID);
                HttpPostedFileBase file = Request.Files[0];
                // thay đổi hình ảnh đại diện
                if (file != null && file.ContentLength > 0)
                {
                    string[] fileExtensions = new string[] { ".jpg", ".png" };
                    if (fileExtensions.Contains(Path.GetExtension(file.FileName).ToLower()))
                    {
                        var fileName = Path.GetFileName(file.FileName);
                        var path = Path.Combine(Server.MapPath("~/Images/HinhKhachHang/"), fileName);
                        file.SaveAs(path);
                        model.Image = $"~/Images/HinhKhachHang/{fileName}";
                        user.Photo = model.Image;
                    }
                    else
                    {
                        ViewBag.MsgError = "Vui lòng chọn hình ảnh sản phẩm có định dạng jpg hoặc png !";
                        return View();
                    }
                }
                // thay đổi password yêu cầu nhập password cũ
                if (model.OldPassword != null)
                {
                    if (!model.OldPassword.Equals(StringUtils.Md5(model.OldPassword)))
                    {
                        ViewBag.MsgError = "Vui lòng nhập đúng mật khẩu cũ !";
                        return View();
                    }
                }
                user.Name = model.Name;
                user.Address = model.Address;

                if (model.NewPassword != null)
                {
                    user.Password = StringUtils.Md5(model.NewPassword);
                }
                // Thay đổi địa chỉ email yêu cầu xác nhận email
                if (!user.Email.Trim().Equals(model.Email))
                {
                    user.Email = model.Email;
                    string code = StringUtils.RandomString(6);
                    user.Code = code;
                    user.Status = 2;
                    db.SaveChanges();
                    Session["user"] = user;
                    StringBuilder str = new StringBuilder();
                    str.Append("<h1>Xác nhận email</h1>");
                    str.Append($"<h3>Mã xác nhận của bạn là: <strong>{code}</strong></h3>");
                    Email.Send(model.Email, "[Thế Giới Ngầm] Xác nhận email", str);
                    return RedirectToAction("ComfirmEmail", "Account");
                }

                db.SaveChanges();
                Session["user"] = user;
                ViewBag.MsgSuccess = "Chỉnh sửa thông tin cá nhân thành công !";
                var viewmodel = new SettingViewModel
                {
                    ID = user.ID,
                    Name = user.Name.Trim(),
                    Email = user.Email,
                    Address = user.Address,
                    Image = user.Photo
                };
                return View(viewmodel);
            }

        }
        /// <summary>
        /// Hiển thị trang cài đặt tài khoản
        /// </summary>
        /// <returns></returns>
        [CheckLogin]
        public ActionResult Setting()
        {
            using (var db = new WebDauGiaEntities())
            {
                int userID = CurrentContext.GetUser().ID;
                var user = db.Users.Find(userID);
                var model = new SettingViewModel {
                    ID = user.ID,
                    Name = user.Name.Trim(),
                    Email = user.Email,
                    Address = user.Address,
                    Image = user.Photo
                };
                return View(model);
            }

        }
        /// <summary>
        /// Hiển thị danh sách bình luận của user
        /// </summary>
        /// <param name="userID"></param>
        /// <returns>Trả về _ListCommentPartialView</returns>
        public ActionResult ListComment(int userID)
        {
            using (var db = new WebDauGiaEntities())
            {
                var vendor = (from c in db.CommentVendors
                              join u in db.Users on c.WinnerID equals u.ID
                              where c.VendorID == userID
                              select new CommentViewModel
                              {
                                  Name = u.Name,
                                  Image = u.Photo,
                                  Comment = c.Comment,
                                  Point = c.Point ?? 0
                              }).ToList();

                var winner = (from c in db.CommentWinners
                              join u in db.Users on c.VendorID equals u.ID
                              where c.WinnerID == userID
                              select new CommentViewModel
                              {
                                  Name = u.Name,
                                  Image = u.Photo,
                                  Comment = c.Comment,
                                  Point = c.Point ?? 0
                              }).ToList();
                var list = vendor;
                list.AddRange(winner);
                return PartialView("_ListCommentPartialView", list);
            }

        }
        /// <summary>
        /// Hiển thị trang cá nhân của user
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult ProfileUser(int? id)
        {
            if (!id.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }
            using (var db = new WebDauGiaEntities())
            {
                var user = db.Users.Find(id);
                var model = new ProfileViewModel
                {
                    ID = user.ID,
                    Name = user.Name,
                    Image = user.Photo,
                    Positive = user.Positive ?? 0,
                    Negative = user.Negative ?? 0,
                    Feedback = (int)(((double)(user.Positive + user.Negative + 1) / (user.Positive + 1)) * 100)
                };
                return View(model);
            }

        }
        /// <summary>
        /// Hiển thị trang lịch sử đấu giá của người dùng
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        [CheckLogin]
        public ActionResult HistoryAuction(int? index)
        {
            if (!index.HasValue)
            {
                index = 1;
            }
            else
            {

            }

            using (var db = new WebDauGiaEntities())
            {
                var uid = CurrentContext.GetUser().ID;
                int n = db.HistoryAuctions.Where(h => h.UserID == uid).Count();
                int take = 6;

                ViewBag.Pages = Math.Ceiling((double)n / take);
                ViewBag.Previous = index - 1 < 1 ? 1 : index - 1;
                ViewBag.Next = index + 1 > ViewBag.Pages ? ViewBag.Pages : index + 1;

                var list = (from h in db.HistoryAuctions
                            join p in db.Products on h.ProductID equals p.ID
                            join img in db.Photos on h.ProductID equals img.ProductID
                            join v in db.Users on p.VendorID equals v.ID
                            where h.UserID == uid
                            select new ProductHistoryViewModel
                            {

                                ID = p.ID,
                                Name = p.Name.Trim(),
                                Image = img.Name,
                                Des = p.Descrition.Trim(),
                                Price = p.Price,
                                PriceAuction = h.Price,
                                BuyNowPice = p.BuyNowPice,
                                DateAuction = h.DateAction,
                                DateEnd = p.DateEnd,
                                WinnerID = p.Winner,
                                Denied = h.Denied,
                                Status = p.Status
                            }).ToList();
                list = list.DistinctBy(p => p.ID).ToList();
                list = list.OrderByDescending(p => p.DateAuction).Skip((int)(index - 1) * take).Take(take).ToList();
                return View(list);
            }
        }
        /// <summary>
        /// Hàm hoàn tiền cho người gửi giá cao nhất có gửi mail
        /// </summary>
        /// <param name="productID"></param>
        public static void Refund(int productID)
        {
            using (var db = new WebDauGiaEntities())
            {
                // lấy thông tin sản phẩm và user
                var pro = db.Products.Find(productID);
                var user = db.Users.Find(pro.Winner);
                // hoàn tiền cho user
                user.Credits += pro.TopPrice;
                db.SaveChanges();
                // lấy đường dẫn của hình sản phẩm
                var image = db.Photos.Where(p => p.ProductID == productID).FirstOrDefault().Name;
                image = image.Substring(2).Trim();
                // khởi tạo nội dung mail
                var body = new StringBuilder();
                body.Append($"<h3>Chào {user.Name} !</h3>");
                body.Append($"<h1 style='color:red;'>Sản phẩm mà bạn đang đấu giá đã có người đấu giá cao hơn.</h1>");
                body.Append($"<h1 style='color:blue'>Sản phẩm {pro.Name}</h1>");
                body.Append($"<img style='width:300px;height:400px;' src=cid:MyPic>");
                body.Append($"<h3 style='color:green;'>Giá đấu hiện tại của sản phẩm: {pro.TopPrice + pro.StepPrice}$</h3>");
                body.Append($"<h3 style='color:red;'>Số tiền bạn được hoàn lại là: {pro.TopPrice}$</h3>");
                body.Append($"<h3>Thời gian kết thúc đấu giá của sản phẩm là: <span style='color:red;'>{pro.DateEnd}</span></h3>");
                body.Append($"<h3>Nếu bạn muốn trở thành người sở hửu sản phẩm hãy nhấn <a href='http://localhost:62902/Product/Detail?productID={pro.ID}'>tại đây.</a></h3>");
                // khởi tạo resource lưu hình ảnh
                LinkedResource yourPictureRes = new LinkedResource(Path.Combine(HttpRuntime.AppDomainAppPath, image));
                yourPictureRes.ContentId = "MyPic";
                // gửi mail
                Email.Send(user.Email, "[Thế Giới Ngầm] Hoàn tiền.", body, yourPictureRes);
            }
        }
        /// <summary>
        /// Hàm thêm tiền cho người bán có gửi mail
        /// </summary>
        /// <param name="productID">id sản phẩm</param>
        /// <param name="index">index = 1 gửi mail user là người chiến thắng; index =2 gửi mail user là người giử giá</param>
        public static void DecreaseCredits(int productID, int index)
        {

            using (var db = new WebDauGiaEntities())
            {
                // lấy thông tin sản phẩm và user
                var pro = db.Products.Find(productID);
                var user = db.Users.Find(pro.Winner);
                // trừ tiền 
                user.Credits -= pro.TopPrice;
                db.SaveChanges();
                // nếu user đang đăng nhập thì cập nhật session
                if (CurrentContext.IsLogged() == true)
                {
                    if (CurrentContext.GetUser().ID == user.ID)
                    {
                        System.Web.HttpContext.Current.Session["user"] = user;
                    }
                }
                // lấy thông tin người đăng
                var vendor = db.Users.Find(pro.VendorID);
                // lấy đường dẫn hình sản phẩm
                var image = db.Photos.Where(p => p.ProductID == productID).FirstOrDefault().Name;
                image = image.Substring(2).Trim();
                // khởi tạo nội dung mail
                var body = new StringBuilder();
                body.Append($"<h3>Chào {user.Name} !</h2>");
                string subject;
                if (index == 1)
                {
                    subject = "Chúc mừng bạn đã chiến thắng đấu giá sản phẩm.";
                    body.Append($"<h1 style='color:blue'>{subject}</h1>");
                }
                else
                {
                    subject = "Chúc mừng bạn đã trở thành người giử giá cao nhất.";
                    body.Append($"<h1 style='color:blue'>{subject}</h1>");
                }
                body.Append($"<h1 style='color:blue'>Sản phẩm {pro.Name}</h1>");
                body.Append($"<img style='width:300px;height:400px;' src=cid:MyPic>");
                if (index == 1)
                {
                    body.Append($"<h3 style='color:blue;'>Bạn đã chiến thắng sản phẩm với giá: {pro.PriceAuction}$</h3>");
                    body.Append($"<h3 >Email của người bán là: <span style='color:red'>{vendor.Email}</span></h3>");
                    body.Append($"<h3 style='color:red;'>Vui lòng liên hệ với người bạn để nhận hàng nếu trong vòng 7 ngày không giao hàng bạn sẽ được hoàn tiền lại.</h3>");
                }
                else
                {
                    body.Append($"<h3 style='color:green;'>Giá đấu hiện tại của sản phẩm: {pro.PriceAuction}$</h3>");
                    body.Append($"<h3 style='color:blue;'>Số tiền bạn đã đấu giá là: {pro.TopPrice}$</h3>");
                    body.Append($"<h3>Thời gian kết thúc đấu giá của sản phẩm là: <span style='color:red;'>{pro.DateEnd}</span></h3>");
                }

                body.Append($"<h3>Bạn có thể xem chi tiết sản phẩm <a href='http://localhost:62902/Product/Detail?productID={pro.ID}'>tại đây.</a></h3>");
                LinkedResource yourPictureRes = new LinkedResource(Path.Combine(HttpRuntime.AppDomainAppPath, image));
                yourPictureRes.ContentId = "MyPic";
                Email.Send(user.Email, $"[Thế Giới Ngầm] {subject}", body, yourPictureRes);
            }

        }
        /// <summary>
        /// Tăng tiền cho người đăng
        /// </summary>
        /// <param name="productID"></param>
        public static void IncreaseCredits(int productID)
        {
            using (var db = new WebDauGiaEntities())
            {
                // lấy thông tin sản phẩm và người đăng
                var pro = db.Products.Find(productID);
                var user = db.Users.Find(pro.VendorID);
                // tăng tiền cho người đăng
                user.Credits += pro.TopPrice;
                db.SaveChanges();
                // lấy thông tin người chiến thắng
                var winner = db.Users.Find(pro.Winner);
                // lấy đường dẫn hình sản phẩm
                var image = db.Photos.Where(p => p.ProductID == productID).FirstOrDefault().Name;
                image = image.Substring(2).Trim();
                // khởi tạo nội dung mail
                var body = new StringBuilder();
                body.Append($"<h3>Chào {user.Name} !</h3>");
                body.Append($"<h1 style='color:green;'>Chúc mừng bạn sản phẩm mà bạn đăng đã có người mua.</h1>");
                body.Append($"<h1 style='color:blue'>Sản phẩm {pro.Name}</h1>");
                body.Append($"<img style='width:300px;height:400px;' src=cid:MyPic>");
                body.Append($"<h3 style='color:green;'>Sản phẩm của bạn được bán với giá: {pro.PriceAuction}$</h3>");
                body.Append($"<h3 style='color:blue;'>Số tiền bạn nhận được là: {pro.PriceAuction}$</h3>");
                body.Append($"<h3 >Email của người thắng là: <span style='color:red'>{winner.Email}</span></h3>");
                body.Append($"<h3 style='color:red;'>Vui lòng liên hệ với người mua để giao hàng nếu trong vòng 7 ngày không giao hàng bạn sẽ bị xóa tài khoản.</h3>");
                body.Append($"<h3>Nếu bạn muốn trở thành người sở hửu sản phẩm hãy nhấn <a href='http://localhost:62902/Product/Detail?productID={pro.ID}'>tại đây.</a></h3>");
                // tạo resource chưa hình ảnh gửi mail
                LinkedResource yourPictureRes = new LinkedResource(Path.Combine(HttpRuntime.AppDomainAppPath, image));
                yourPictureRes.ContentId = "MyPic";
                Email.Send(user.Email, "[Thế Giới Ngầm] Chúc mừng bạn đã bán sản phẩm thành công.", body, yourPictureRes);
            }
        }
        /// <summary>
        /// Gửi email thông báo người đăng
        /// </summary>
        /// <param name="productID"></param>
        /// <param name="index">index =0 thông báo sản phẩm đã kết thúc và không có người mua;index =1 sản phẩm đã có người đấu giá</param>
        public static void GuiMailNguoiBan(int productID,int index)
        {
            using (var db = new WebDauGiaEntities())
            {
                var pro = db.Products.Find(productID);
                var user = db.Users.Find(pro.VendorID);
                var image = db.Photos.Where(p => p.ProductID == productID).FirstOrDefault().Name;
                image = image.Substring(2).Trim();
                var body = new StringBuilder();
                body.Append($"<h3>Chào {user.Name} !</h3>");
                string subject;
                if (index == 0)
                {
                    subject = "Sản phẩm của bạn đăng đã hết hạn và không có người đấu giá.";
                    body.Append($"<h1 style='color:blue;'>{subject}</h1>");
                    body.Append($"<h1><span style='color:blue;'>Sản phẩm {pro.Name} </span> của bạn đang đăng.</h1>");
                    body.Append($"<img style='width:300px;height:400px;' src=cid:MyPic>");
                }
                else
                {
                    subject = "Chúc mừng bạn của bạn đang đăng đã có người đấu giá.";
                    body.Append($"<h1 style='color:blue;'>{subject}</h1>");
                    body.Append($"<h1><span style='color:blue;'>Sản phẩm {pro.Name} </span> </h1>");
                    body.Append($"<img style='width:300px;height:400px;' src=cid:MyPic>");
                    body.Append($"<h3 style='color:blue;'>Đã có người đấu với giá: {pro.PriceAuction}$</h3>");
                    body.Append($"<h3>Thời gian kết thúc đấu giá của sản phẩm là: <span style='color:red;'>{pro.DateEnd}</span></h3>");
                    body.Append($"<h3>Bạn có thể xem chi tiết sản phẩm <a href='http://localhost:62902/Product/Detail?productID={pro.ID}'>tại đây</a></h3>");
                }
                
                LinkedResource yourPictureRes = new LinkedResource(Path.Combine(HttpRuntime.AppDomainAppPath, image));
                yourPictureRes.ContentId = "MyPic";
                Email.Send(user.Email, $"[Thế Giới Ngầm] {subject}", body, yourPictureRes);
            }
        }
        /// <summary>
        /// Đấu giá
        /// </summary>
        /// <param name="productID"></param>
        /// <param name="auction"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Auction(int productID, int auction)
        {
            string error=null,success=null;
            //kiểm tra user có đủ credits không
            if (CurrentContext.GetUser().Credits < auction)
            {
                error = "Bạn không đủ Credits để đấu giá !";
            }
            else
            {
                using (var db = new WebDauGiaEntities())
                {
                    //Thêm vào lịch sử đấu giá của sản phẩm
                    var pro = db.Products.Find(productID);
                    var userID = CurrentContext.GetUser().ID;
                    var auc = new HistoryAuction();
                    auc.UserID = userID;
                    auc.ProductID = productID;
                    auc.Price = auction;
                    auc.DateAction = DateTime.Now;
                    auc.Denied = 0;
                    db.HistoryAuctions.Add(auc);
                    db.SaveChanges();
                    // nếu giá đấu < giá đấu cao nhất thông báo cho user
                    // giá hiện tại sẽ = giá đấu + bước giá 
                    if (auction <= pro.TopPrice)
                    {
                        pro.PriceAuction = auction + pro.StepPrice;
                        db.SaveChanges();
                        error = "Đã có người đấu giá cao hơn bạn !";
                    }
                    else
                    {
                        // ngược lại giá hoàn tiền cho người gửi giá trước đó
                        if (pro.Winner != pro.VendorID)
                        {
                            Refund(productID);
                        }
                        // giá hiện tại = giá đấu của người gửi giá trước đó + bước giá
                        pro.PriceAuction= pro.TopPrice + pro.StepPrice;
                        // giá đấu cao nhất của sp  = giá đấu của user
                        pro.TopPrice = auction;
                        pro.Winner = userID;
                        // nếu người đăng có mua chức năng tự gia hạn
                        // hệ thống sẽ gia hạn thêm 10p khi còn dưới 5p mà có người tham gia đấu giá
                        if (pro.AutoExtend == true)
                        {
                            if (DateTime.Now >= pro.DateEnd.Value.AddMinutes(-5) && DateTime.Now < pro.DateEnd.Value)
                            {
                                pro.DateEnd = pro.DateEnd.Value.AddMinutes(10);
                            }
                        }

                        db.SaveChanges();
                        // trừ tiền người đấu giá
                        DecreaseCredits(productID,0);
                        // gửi mail người đăng
                        GuiMailNguoiBan(productID,1);
                        success = "Chúc mừng bạn đã đấu giá thành công !";
                    }
                }
            }
            return RedirectToAction("Detail", "Product", new { productID = productID, msgerror = error,msgsuccess=success });
        }
        /// <summary>
        /// Hàm thêm sản phẩm vào danh sách yêu thích
        /// </summary>
        /// <param name="productID"></param>
        /// <param name="userID"></param>
        public void Watching(int productID, int userID)
        {
            using (var db = new WebDauGiaEntities())
            {
                // tìm xem sản phẩm đã có trong danh sách sản phẩm yêu thích của user chưa
                var watch = db.WatchLists.Where(w => w.ProductID == productID && w.UserID == userID).FirstOrDefault();
                // nếu chưa có thì thêm vào
                if (watch == null)
                {
                    watch = new WatchList
                    {
                        ProductID = productID,
                        UserID = userID
                    };
                    db.WatchLists.Add(watch);
                }
                else
                {
                    // ngược lại nếu có rồi thì xóa
                    db.Entry(watch).State = System.Data.Entity.EntityState.Deleted;
                }

                db.SaveChanges();
            }
        }

        // Post: Account/Logout
        /// <summary>
        /// Đăng xuất
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Logout()
        {
            CurrentContext.Destroy();
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Login
        /// <summary>
        /// Hiển thị trang Đăng nhập
        /// </summary>
        /// <returns></returns>
        public ActionResult Login()
        {
            if (CurrentContext.IsLogged())
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return View();
            }

        }
        /// <summary>
        /// Đăng nhập
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        // POST: Account/Login
        [HttpPost]
        public ActionResult Login(LoginViewModel model)
        {
            string encPwd = StringUtils.Md5(model.Password);
            using (var db = new WebDauGiaEntities())
            {
                var user = db.Users.Where(u => u.UserName == model.Username && u.Password == encPwd).FirstOrDefault();
                if (user != null)
                {
                    Session["isLogin"] = 1;
                    Session["user"] = user;

                    if (Request.Form.GetValues("Remember") != null)
                    {
                        Response.Cookies["userID"].Value = user.ID.ToString();
                        Response.Cookies["userID"].Expires = DateTime.Now.AddDays(7);
                    }

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ViewBag.ErrorMsg = "Tên đăng nhập hoặc tài khoản không đúng !";
                    return View();
                }
            }

        }

        /// <summary>
        /// Hiển thị trang xác thực email
        /// </summary>
        /// <returns></returns>
        [CheckLogin]
        public ActionResult ComfirmEmail()
        {
            return View();
        }
        /// <summary>
        /// Xác thực email
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ComfirmEmail(ComfirmEmailViewModel model)
        {
            using (var db = new WebDauGiaEntities())
            {
                var user = db.Users.Find(model.UserID);
                if (String.Compare(user.Code, model.Code, true) == 0)
                {
                    user.Status = 1;
                    Session["user"] = user;
                    db.SaveChanges();
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ViewBag.ErrorMsg = "Mã xác nhận không đúng. Vui lòng kiểm tra lại email!";
                    return View();
                }
            }
        }
        /// <summary>
        /// Hiển thị trang Đăng ký
        /// </summary>
        /// <returns></returns>
        // GET: Account/Register
        public ActionResult Register()
        {
            if (CurrentContext.IsLogged())
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return View();
            }
        }
        /// <summary>
        /// Kiểm tra email không trùng
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public int CheckEmail(string email)
        {
            using (var db= new WebDauGiaEntities())
            {
                var model = db.Users.Where(u => u.Email == email).FirstOrDefault();
                if (model == null)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }           
        }
        // Post: Account/Register
        /// <summary>
        /// Đăng ký tài khoản
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        // kiểm tra captcha
        [CaptchaValidation("CaptchaCode", "ExampleCaptcha", "Incorrect CAPTCHA code!")]
        public ActionResult Register(RegisterViewModel model)
        {
            //kiểm tra email đã sử dụng hay chưa
            if (CheckEmail(model.Email)==0)
            {
                ViewBag.ErrorMsg = "Email đã tồn tại!";
                return View();
            }
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMsg = "Incorrect CAPTCHA code!";
                return View();
            }
            else
            {
                // khởi tạo mã xác thực
                string code = StringUtils.RandomString(6);
                // khởi tạo user
                User u = new User
                {
                    UserName = model.Username,
                    Password = StringUtils.Md5(model.RawPassword),
                    Name = model.FullName,
                    Email = model.Email,
                    Credits = 500,
                    Address = model.Address,
                    Role = 0,
                    Photo = "~/images/HinhKhachHang/avt.png",
                    Positive = 0,
                    Negative = 0,
                    Status = 2,
                    SaleStatus = 0,
                    DateStartSale = DateTime.Now.AddDays(-1),
                    DateEndSale=DateTime.Now.AddDays(-1),
                    Code = code
                };
                using (var data = new WebDauGiaEntities())
                {
                    data.Users.Add(u);
                    data.SaveChanges();
                }
                //lưu session
                Session["isLogin"] = 1;
                Session["user"] = u;
                // khởi tạo nội dung email
                StringBuilder str = new StringBuilder();
                str.Append("<h1>Xác nhận email</h1>");
                str.Append($"<h3>Chào {u.Name} !</h1>");
                str.Append($"<h3>Mã xác nhận của bạn là: <strong>{code}</strong></h3>");
                str.Append($"<h3>Bạn có thể xem chi tiết sản phẩm <a href='http://localhost:62902/Account/ComfirmEmail'>tại đây.</a></h3>");
                Email.Send(u.Email, "[Thế Giới Ngầm] Xác nhận email", str);
                return RedirectToAction("ComfirmEmail", "Account");
            }
        }
    }
}