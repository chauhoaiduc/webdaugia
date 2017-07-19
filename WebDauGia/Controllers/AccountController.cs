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
        [CheckLogin]
        public ActionResult DeniedVendor(int vendorID, int userID,int proID)
        {
            string notify;

            using (var db = new WebDauGiaEntities())
            {
                var listpro = db.Products.Where(p => p.VendorID == vendorID).ToList();
                foreach(var item in listpro)
                {
                    var denied = new DeniedProduct
                    {
                        ProductID = item.ID,
                        UserID = userID
                    };
                    db.DeniedProducts.Add(denied);

                    var listuser = db.HistoryAuctions.Where(h => h.UserID == userID && h.ProductID == item.ID).ToList();

                    foreach (var temp in listuser)
                    {
                        temp.Denied = 1;
                    }
                    db.SaveChanges();

                    var pro = db.Products.Find(item.ID);

                    if (userID == pro.Winner)
                    {
                        var history = db.HistoryAuctions.Where(h => h.ProductID == item.ID && h.Denied != 1).ToList();
                        history = history.OrderByDescending(h => h.Price).ToList();
                        history = history.DistinctBy(h => h.UserID).ToList();

                        int flag = 0;
                        if (history.Count > 1)
                        {
                            for (int i = 0; i < history.Count - 1; i++)
                            {
                                var user = db.Users.Find(history[i].UserID);
                                if (user.Credits > history[i].Price)
                                {
                                    flag = 1;
                                    if (i + 1 == history.Count)
                                    {
                                        pro.PriceAuction = pro.Price;
                                    }
                                    else
                                    {
                                        pro.PriceAuction = history[i + 1].Price;
                                    }

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

        [HttpPost]
        public ActionResult DeniedProduct(int productID, int userID)
        {
            string notify;

            using (var db = new WebDauGiaEntities())
            {
                var denied = new DeniedProduct
                {
                    ProductID = productID,
                    UserID = userID
                };
                db.DeniedProducts.Add(denied);

                var listuser = db.HistoryAuctions.Where(h => h.UserID == userID && h.ProductID == productID).ToList();

                foreach (var temp in listuser)
                {
                    temp.Denied = 1;
                }
                db.SaveChanges();

                var pro = db.Products.Find(productID);
                HoanTien(productID);
                if (userID == pro.Winner)
                {
                    var history = db.HistoryAuctions.Where(h => h.ProductID == productID && h.Denied != 1).ToList();
                    history = history.OrderByDescending(h => h.Price).ToList();
                    history = history.DistinctBy(h => h.UserID).ToList();

                    int flag = 0;
                    if (history.Count > 1)
                    {
                        for (int i = 0; i < history.Count - 1; i++)
                        {
                            var user = db.Users.Find(history[i].UserID);
                            if (user.Credits > history[i].Price)
                            {
                                flag = 1;
                                if (i + 1 == history.Count)
                                {
                                    pro.PriceAuction = pro.Price;
                                }
                                else
                                {
                                    pro.PriceAuction = history[i + 1].Price;
                                }

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
                TruTien(productID,0);
                notify = "Cấm quyền user thành công !";
            }

            return RedirectToAction("Detail", "Product", new { productID = productID, msgsuccess = notify });


        }
        public ActionResult BecomeSeller()
        {
            return View();
            
        }
        [HttpPost]
        public ActionResult Seller()
        {
            var userID = CurrentContext.GetUser().ID;
            using (var db = new WebDauGiaEntities())
            {
                var user = db.Users.Find(userID);
                user.SaleStatus = 1;
                user.Credits += 100;
                db.SaveChanges();
                Session["user"] = user;
                ViewBag.MsgSuccess = "Bạn đã mua chức năng thành vui lòng đợi admin duyệt.";
                return View("BecomeSeller");
            }
        }
        public static void CheckTimeOut(WebDauGiaEntities db)
        {
            var list = db.Products;
            foreach (var item in list)
            {
                if (item.Status == 1)
                {
                    if (item.DateEnd < DateTime.Now)
                    {
                        item.Status = 2;
                        if (item.Winner != item.VendorID)
                        {
                            TruTien(item.ID, 1);
                            ThemTienNguoiBan(item.ID);
                        }
                        else
                        {
                            GuiMailNguoiBan(item.ID, 0);
                        }

                    }
                }
            }
            db.SaveChanges();
        }
        // Het Thời Gian Đấu Giá
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
                    TruTien(ID, 1);
                    ThemTienNguoiBan(ID);
                }
                else
                {
                    GuiMailNguoiBan(ID, 0);
                }
                return RedirectToAction("Index", "Home");
            }
                
        }
        [HttpPost]
        [CheckLogin]
        public ActionResult BuyItNow(int productID)
        {
            using (var db = new WebDauGiaEntities())
            {
                var pro = db.Products.Find(productID);
                var uc = CurrentContext.GetUser();
                if (uc.Credits < pro.BuyNowPice)
                {
                    string notify = "Xin lỗi bạn không đủ tiền mua sản phẩm này";
                    return RedirectToAction("Detail", "Product", new { productID = productID, msgerror = notify });
                }
                else
                {
                    HoanTien(productID);
                    pro.TopPrice = pro.BuyNowPice;
                    pro.PriceAuction = pro.BuyNowPice;
                    pro.Winner = CurrentContext.GetUser().ID;
                    pro.Status = 2;
                    db.SaveChanges();
                    TruTien(productID, 1);
                    ThemTienNguoiBan(productID);
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
        [HttpPost]
        [CheckLogin]
        [ValidateInput(false)]
        public ActionResult UploadItem(UploadItemViewModel pro)
        {
            using (var db = new WebDauGiaEntities())
            {
                var list = new SelectList(db.Categories.ToList(), "ID", "Name");

                var product = new Product();
                //Create photos product
                int flag = 0;
                for (int i = 0; i < Request.Files.Count; i++)
                {
                    HttpPostedFileBase file = Request.Files[i];
                    if (file != null && file.ContentLength > 0)
                    {
                        flag = 1;
                        string[] fileExtensions = new string[] { ".jpg", ".png" };

                        if (fileExtensions.Contains(Path.GetExtension(file.FileName).ToLower()))
                        {
                            var fileName = Path.GetFileName(file.FileName);
                            var path = Path.Combine(Server.MapPath($"~/Images/{pro.CategoryID}/"), fileName);
                            file.SaveAs(path);
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

                //Create product
                int userID = CurrentContext.GetUser().ID;
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
        [CheckLogin]
        [HttpPost]
        public ActionResult CommentWinner(CommentUserViewModel model)
        {
            int userID = CurrentContext.GetUser().ID;
            using (var db = new WebDauGiaEntities())
            {
                var cm = new CommentWinner
                {
                    ProductID = model.ProductID,
                    VendorID = userID,
                    WinnerID = model.UserID,
                    Comment = model.Comment,
                    Point = model.Point
                };
                db.CommentWinners.Add(cm);
                var user = db.Users.Find(model.UserID);
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
        [CheckLogin]
        [HttpPost]
        public ActionResult CommentVendor(CommentUserViewModel model)
        {
            int userID = CurrentContext.GetUser().ID;
            using (var db = new WebDauGiaEntities())
            {
                var cm = new CommentVendor
                {
                    ProductID = model.ProductID,
                    VendorID = model.UserID,
                    WinnerID = userID,
                    Comment = model.Comment,
                    Point = model.Point
                };
                db.CommentVendors.Add(cm);
                var user = db.Users.Find(model.UserID);
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
        [HttpPost]
        public ActionResult Setting(SettingViewModel model)
        {
            using (var db = new WebDauGiaEntities())
            {
                var user = db.Users.Find(CurrentContext.GetUser().ID);
                HttpPostedFileBase file = Request.Files[0];
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
        public static void HoanTien(int productID)
        {
            using (var db = new WebDauGiaEntities())
            {
                var pro = db.Products.Find(productID);
                var user = db.Users.Find(pro.Winner);
                user.Credits += pro.TopPrice;
                db.SaveChanges();
                var image = db.Photos.Where(p => p.ProductID == productID).FirstOrDefault().Name;
                image = image.Substring(2).Trim();
                var body = new StringBuilder();
                body.Append($"<h3>Chào {user.Name} !</h3>");
                body.Append($"<h1 style='color:red;'>Sản phẩm mà bạn đang đấu giá đã có người đấu giá cao hơn.</h1>");
                body.Append($"<h1 style='color:blue'>Sản phẩm {pro.Name}</h1>");
                body.Append($"<img style='width:300px;height:400px;' src=cid:MyPic>");
                body.Append($"<h3 style='color:green;'>Giá đấu hiện tại của sản phẩm: {pro.TopPrice + pro.StepPrice}$</h3>");
                body.Append($"<h3 style='color:red;'>Số tiền bạn được hoàn lại là: {pro.TopPrice}$</h3>");
                body.Append($"<h3>Thời gian kết thúc đấu giá của sản phẩm là: <span style='color:red;'>{pro.DateEnd}</span></h3>");
                body.Append($"<h3>Nếu bạn muốn trở thành người sở hửu sản phẩm hãy nhấn <a href='http://localhost:62902/Product/Detail?productID={pro.ID}'>tại đây.</a></h3>");
                LinkedResource yourPictureRes = new LinkedResource(Path.Combine(HttpRuntime.AppDomainAppPath, image));
                yourPictureRes.ContentId = "MyPic";
                Email.Send(user.Email, "[Thế Giới Ngầm] Hoàn tiền.", body, yourPictureRes);
            }
        }

        public static void TruTien(int productID,int index)
        {
            using (var db = new WebDauGiaEntities())
            {
                var pro = db.Products.Find(productID);
                var user = db.Users.Find(pro.Winner);
                user.Credits -= pro.TopPrice;
                db.SaveChanges();
                if (CurrentContext.GetUser().ID == user.ID)
                {
                    System.Web.HttpContext.Current.Session["user"] = user;
                }
                var vendor = db.Users.Find(pro.VendorID);
                var image = db.Photos.Where(p => p.ProductID == productID).FirstOrDefault().Name;
                image = image.Substring(2).Trim();
                var body = new StringBuilder();
                body.Append($"<h3>Chào {user.Name} !</h2>");
                string subject;
                if (index==1)
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
                if (index==1)
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

        public static void ThemTienNguoiBan(int productID)
        {
            using (var db = new WebDauGiaEntities())
            {
                var pro = db.Products.Find(productID);
                var user = db.Users.Find(pro.VendorID);
                user.Credits += pro.TopPrice;
                db.SaveChanges();
                var winner = db.Users.Find(pro.Winner);
                var image = db.Photos.Where(p => p.ProductID == productID).FirstOrDefault().Name;
                image = image.Substring(2).Trim();
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
                LinkedResource yourPictureRes = new LinkedResource(Path.Combine(HttpRuntime.AppDomainAppPath, image));
                yourPictureRes.ContentId = "MyPic";
                Email.Send(user.Email, "[Thế Giới Ngầm] Chúc mừng bạn đã bán sản phẩm thành công.", body, yourPictureRes);
            }
        }

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

        [HttpPost]
        public ActionResult Auction(int productID, int auction)
        {
            string error=null,success=null;

            if (CurrentContext.GetUser().Credits < auction)
            {
                error = "Bạn không đủ Credits để đấu giá !";
            }
            else
            {
                using (var db = new WebDauGiaEntities())
                {
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

                    if (auction <= pro.TopPrice)
                    {
                        pro.PriceAuction = auction + pro.StepPrice;
                        db.SaveChanges();
                        error = "Đã có người đấu giá cao hơn bạn !";
                    }
                    else
                    {
                        if (pro.Winner != pro.VendorID)
                        {
                            HoanTien(productID);
                        }

                        pro.PriceAuction= pro.TopPrice + pro.StepPrice;
                        pro.TopPrice = auction;
                        pro.Winner = userID;

                        if (pro.AutoExtend == true)
                        {
                            if (DateTime.Now >= pro.DateEnd.Value.AddMinutes(-5) && DateTime.Now < pro.DateEnd.Value)
                            {
                                pro.DateEnd = pro.DateEnd.Value.AddMinutes(10);
                            }
                        }

                        db.SaveChanges();
                        TruTien(productID,0);
                        GuiMailNguoiBan(productID,1);
                        success = "Chúc mừng bạn đã đấu giá thành công !";
                    }
                }
            }
            return RedirectToAction("Detail", "Product", new { productID = productID, msgerror = error,msgsuccess=success });
        }
        public void Watching(int productID, int userID)
        {
            using (var db = new WebDauGiaEntities())
            {
                var watch = db.WatchLists.Where(w => w.ProductID == productID && w.UserID == userID).FirstOrDefault();
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
                    db.Entry(watch).State = System.Data.Entity.EntityState.Deleted;
                }

                db.SaveChanges();
            }
        }

        // Post: Account/Logout
        [HttpPost]
        public ActionResult Logout()
        {
            CurrentContext.Destroy();
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Login
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


        [CheckLogin]
        public ActionResult ComfirmEmail()
        {
            return View();
        }
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
        [HttpPost]
        [CaptchaValidation("CaptchaCode", "ExampleCaptcha", "Incorrect CAPTCHA code!")]
        public ActionResult Register(RegisterViewModel model)
        {
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
                string code = StringUtils.RandomString(6);
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
                Session["isLogin"] = 1;
                Session["user"] = u;
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