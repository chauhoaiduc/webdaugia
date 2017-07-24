using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using WebDauGia.Models;
using WebDauGia.Filters;
using Microsoft.Ajax.Utilities;
using System.Text;
using WebDauGia.Helper;
using System.Net.Mail;
using System.IO;

namespace WebDauGia.Controllers
{
    public class AdminController : Controller
    {
        /// <summary>
        /// Duyệt cho phép người dùng trở thành người đăng sản phẩm
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ApproveSale(int userID)
        {
            using (var db = new WebDauGiaEntities())
            {
                var user = db.Users.Find(userID);
                user.SaleStatus = 0;
                user.DateStartSale = DateTime.Now;
                user.DateEndSale = DateTime.Now.AddDays(7);
                db.SaveChanges();
                string notify = "Đã duyệt thành công !";
                // khởi tạo nội dung mail
                var body = new StringBuilder();
                body.Append($"<h3>Chào {user.Name} !</h2>");
                body.Append($"<h1 style='color:blue'>Chúc mừng bạn đã trở thành người bán hàng.</h1>");

                body.Append($"<h3>Bạn sẽ được đăng sản phẩm trong vòng 7 ngày kể từ ngày được duyệt.</h3>");
                body.Append($"<h3>Thời gian bắt đầu: <span style='color:red;'>{user.DateEndSale}</span></h3>");
                body.Append($"<h3>Thời gian kết thúc: <span style='color:red;'>{user.DateEndSale}</span></h3>");

                Email.Send(user.Email, "[Thế Giới Ngầm] Chúc mừng bạn đã trở thành người bán hàng.", body);
                return RedirectToAction("ApproveSale", "Admin", new {message=notify});
            }
        }
        /// <summary>
        /// Hiển thị trang duyệt người dùng trở thành người đăng sản phẩm có phân trang
        /// </summary>
        /// <param name="index"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        [CheckLoginAdmin]
        public ActionResult ApproveSale(int? index,string message)
        {
            ViewBag.Message = message;
            if (!index.HasValue)
            {
                index = 1;
            }
            using (var db = new WebDauGiaEntities())
            {
                var list = (from u in db.Users
                            where u.SaleStatus == 1
                            select new ManageUserViewModel
                            {
                                ID = u.ID,
                                Name = u.Name,
                                UserName = u.UserName,
                                Address = u.Address,
                                Credits = u.Credits,
                                Email = u.Email,
                                Negative = u.Negative,
                                Positive = u.Positive,
                                Image = u.Photo
                            }).ToList();
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
        /// Thêm thể loại
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckLoginAdmin]
        public ActionResult AddCategory(Category item)
        {
            using (var db = new WebDauGiaEntities())
            {
                item.Status = true;
                db.Categories.Add(item);
                db.SaveChanges();
                string spDirPath = Server.MapPath("~/images/products");
                string targetDirpath = Path.Combine(spDirPath, item.ID.ToString());
                Directory.CreateDirectory(targetDirpath);

                string notify = "Đã thêm thể loại thành công !";
                return RedirectToAction("ManageCategory", new { message = notify });
            }

        }
        /// <summary>
        /// Xóa thể loại
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // POST: Admin/EditCategory
        [HttpPost]
        [CheckLoginAdmin]
        public ActionResult RemoveCategory(int id)
        {
            using (var db = new WebDauGiaEntities())
            {
                var cate = db.Categories.Find(id);
                cate.Status = false;
                db.SaveChanges();
                string notify = "Đã xóa thể loại thành công !";
                return RedirectToAction("ManageCategory", new { message = notify });
            }

        }
        /// <summary>
        /// Chỉnh sửa thông tin thể loại
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [CheckLoginAdmin]
        public ActionResult EditCategory(int? id)
        {
            if (!id.HasValue)
            {
                return View("Error");
            }
            else
            {
                using (var db = new WebDauGiaEntities())
                {
                    var cate = db.Categories.Find(id);
                    return View(cate);
                }
            }

        }
        /// <summary>
        /// Hiển thị trang chỉnh sửa thông tin thể loại
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult EditCategory(Category item)
        {
            using (var db = new WebDauGiaEntities())
            {
                var cate = db.Categories.Find(item.ID);
                cate.Name = item.Name;
                db.SaveChanges();
                ViewBag.Success = "Chỉnh sửa tên thể loại thành công !";
                return View(cate);
            }

        }
        /// <summary>
        /// hiển thị trang quản lý thể loại
        /// </summary>
        /// <param name="message"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        [CheckLoginAdmin]
        public ActionResult ManageCategory(string message, int? index)
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
                var list = db.Categories.Where(u => u.Status == true).ToList();
                int n = list.Count();
                int take = 6;

                ViewBag.Pages = Math.Ceiling((double)n / take);
                ViewBag.Previous = index - 1 < 1 ? 1 : index - 1;
                ViewBag.Next = index + 1 > ViewBag.Pages ? ViewBag.Pages : index + 1;

                list = list.OrderBy(c => c.ID).Skip((int)(index - 1) * take).Take(take).ToList();
                return View(list);
            }
        }
        // POST: ResetPassword
        /// <summary>
        ///  Reser password user
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckLoginAdmin]
        public ActionResult ResetPasswordUser(int? userID)
        {
            if (!userID.HasValue)
            {
                return View("Error");
            }
            else
            {
                using (var db = new WebDauGiaEntities())
                {
                    var user = db.Users.Find(userID);
                    // gửi pasword mới cho user
                    var newPWD = StringUtils.RandomString(6);
                    var body = new StringBuilder();
                    body.Append($"<h3>Chào {user.Name} !</h2>");
                    body.Append($"<h1 style='color:blue;'>Password của bạn đã được reset.</h1>");
                    body.Append($"<h3 >Password mới của bạn là: <span style='color:red;'>{newPWD}<span></h3>");

                    Email.Send(user.Email, "[Thế Giới Ngầm] Reset password.", body);

                    user.Password = StringUtils.Md5(newPWD);
                    db.SaveChanges();
                    
                    string notify = "Reset password thành công . Password mới đã được gửi vào email của User!";

                    return RedirectToAction("ManageUser", new { message = notify });
                }
            }
        }
        /// <summary>
        /// Xóa user
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RemoveUser(int? userID)
        {
            if (!userID.HasValue)
            {
                return View("Error");
            }
            using (var db = new WebDauGiaEntities())
            {
                var user = db.Users.Find(userID);
                user.Status = 0;
                db.SaveChanges();

                var body = new StringBuilder();
                body.Append($"<h3>Chào {user.Name} !</h2>");
                body.Append($"<h1 style='color:red;'>Cảnh báo tài khoản của bạn đã bị xóa.</h1>");
                body.Append($"<h3 style='color:red;'>Vì một số lý do tài khoản của bạn đã bị xóa.</h3>");
                Email.Send(user.Email, "[Thế Giới Ngầm] Cảnh báo tài khoản của bạn đã bị xóa.", body);
                string notify = "Đã xóa user thành công !";

                return RedirectToAction("ManageUser", new { message = notify });
            }
        }
        /// <summary>
        /// Hiển thị trang quản lý user có phân trang
        /// </summary>
        /// <param name="index"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        [CheckLoginAdmin]
        public ActionResult ManageUser(int? index,string message)
        {
            ViewBag.Message = message;
            if (!index.HasValue)
            {
                index = 1;
            }
            using (var db = new WebDauGiaEntities())
            {
                var list = (from u in db.Users
                            where u.Status == 1
                            select new ManageUserViewModel
                            {
                                ID=u.ID,
                                Name=u.Name,
                                UserName=u.UserName,
                                Address=u.Address,
                                Credits=u.Credits,
                                Email=u.Email,
                                Negative=u.Negative,
                                Positive=u.Positive,
                                Image=u.Photo
                            }).ToList();
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
        /// Duyệt cho user đăng sản phẩm
        /// </summary>
        /// <param name="productID"></param>
        /// <param name="vendorID"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Approve(int productID,int vendorID)
        {
            using (var db = new WebDauGiaEntities())
            {
                var pro = db.Products.Find(productID);
                pro.Status = 1;
                pro.DateEnd = DateTime.Now.AddDays(7);
                db.SaveChanges();
                //khởi tạo nội dung mail
                var user = db.Users.Find(vendorID);
                var image = db.Photos.Where(p => p.ProductID == productID).FirstOrDefault().Name;
                image = image.Substring(2).Trim();
                var body = new StringBuilder();
                body.Append($"<h3>Chào {user.Name} !</h2>");
                body.Append($"<h1 style='color:blue'>Chúc mừng bạn của bạn đăng đã được duyệt.</h1>");
                body.Append($"<h1 style='color:blue'>Sản phẩm {pro.Name}</h1>");
                body.Append($"<img style='width:300px;height:400px;' src=cid:MyPic>");
                body.Append($"<h3 style='color:green;'>Ngày đăng của sản phẩm: {pro.DateStart}$</h3>");
                body.Append($"<h3>Sản phẩm sẽ được đăng trong vòng 7 ngày kể từ ngày được duyệt.</h3>");
                body.Append($"<h3>Thời gian kết thúc đấu giá của sản phẩm là: <span style='color:red;'>{pro.DateEnd}</span></h3>");
                body.Append($"<h3>Bạn có thể xem chi tiết sản phẩm <a href='http://localhost:62902/Product/Detail?productID={pro.ID}'>tại đây.</a></h3>");
                LinkedResource yourPictureRes = new LinkedResource(Path.Combine(HttpRuntime.AppDomainAppPath, image));
                yourPictureRes.ContentId = "MyPic";
                Email.Send(user.Email, "[Thế Giới Ngầm] Chúc mừng bạn sản phẩm đã được duyệt.", body, yourPictureRes);

                string notify = "Duyệt sản phẩm thành công !";
                return RedirectToAction("Approve", "Admin",new { message=notify});
            }
                
        }
        /// <summary>
        /// Hiển thị trang duyệt cho user đăng sản phẩm
        /// </summary>
        /// <param name="index"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        [CheckLoginAdmin]
        public ActionResult Approve(int? index,string message)
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
                var list = (from p in db.Products
                            join v in db.Users on p.VendorID equals v.ID
                            join photo in db.Photos on p.ID equals photo.ProductID
                            where p.Status == 0
                            select new ProductApproveViewModel
                            {
                                ID = p.ID,
                                Name = p.Name,
                                VendorID=p.VendorID,
                                VendorName = v.Name,
                                Image = photo.Name,
                                Des = p.Descrition,
                                Price = p.Price,
                                StepPrice=p.StepPrice,
                                DateStart = p.DateStart,
                                BuyNowPice = p.BuyNowPice,
                                AutoExtend = p.AutoExtend
                            }).ToList();
                list = list.DistinctBy(p => p.ID).ToList();

                int n = list.Count();
                int take = 6;

                ViewBag.Pages = Math.Ceiling((double)n / take);
                ViewBag.Previous = index - 1 < 1 ? 1 : index - 1;
                ViewBag.Next = index + 1 > ViewBag.Pages ? ViewBag.Pages : index + 1;

                list=list.OrderBy(p => p.DateStart).Skip((int)(index - 1) * take).Take(take).ToList();
                return View(list);
            }
        }
    }
}