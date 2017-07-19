using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using WebDauGia.Models;
namespace WebDauGia.Helper
{
    public class CurrentContext
    {
        public static string Hidden(string str)
        {
            StringBuilder res=new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                if (i % 2 == 0)
                {
                    res.Append("*");
                }
                else
                {
                    res.Append(str[i]);
                }
            }
            return res.ToString();
        }
        public static CommentWinner IsCommentWinner(int winnerID, int productID)
        {
            int userID = GetUser().ID;
            using (var db = new WebDauGiaEntities())
            {
                var cm = db.CommentWinners.Where(c => c.ProductID == productID && c.WinnerID == winnerID && c.VendorID == userID).FirstOrDefault();
                if (cm != null)
                {
                    return cm;
                }
                else
                {
                    return null;
                }
            }
        }
        public static CommentVendor IsCommentVendor(int vendorID, int productID)
        {
            int userID = GetUser().ID;
            using (var db = new WebDauGiaEntities())
            {
                var cm = db.CommentVendors.Where(c => c.ProductID == productID && c.VendorID == vendorID && c.WinnerID == userID).FirstOrDefault();
                if (cm != null)
                {
                    return cm;
                }
                else
                {
                    return null;
                }
            }
        }
        public static int Feedback()
        {
            var user = GetUser();
            var item = (int)(((double)(user.Positive + user.Negative + 1)/(user.Positive + 1)) * 100);
            return item;
        }
        public static bool IsLogged()
        {
            var flag = HttpContext.Current.Session["isLogin"];
            if (flag == null || (int)flag == 0)
            {
                // Kiểm tra trong cookie
                // nếu có cookie , dùng thông tin trong cookie
                // để tái tạo lại session
                if (HttpContext.Current.Request.Cookies["userID"] != null)
                {
                    int userIdCookie = Convert.ToInt32(HttpContext.Current.Request.Cookies["userID"].Value);
                    using (var db = new WebDauGiaEntities())
                    {
                        var user = db.Users.Where(u => u.ID == userIdCookie).FirstOrDefault();
                        HttpContext.Current.Session["isLogin"] = 1;
                        HttpContext.Current.Session["user"] = user;
                        return true;
                    }
                }
                else
                {
                    return false;
                }

            }
            else
            {
                return true;
            }
        }

        public static User GetUser()
        {
            return (User)HttpContext.Current.Session["user"];
        }

        
        public static void Destroy()
        {
            HttpContext.Current.Session["isLogin"] = 0;
            HttpContext.Current.Session["user"] = null;
            if (HttpContext.Current.Request.Cookies["userID"] != null)
            {
                HttpContext.Current.Response.Cookies["userID"].Expires = DateTime.Now.AddDays(-1);
            }
        }
    }
}