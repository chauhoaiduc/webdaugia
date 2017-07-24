using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebDauGia.Models;

namespace WebDauGia.Controllers
{
    public class CategoryController : Controller
    {
        /// <summary>
        /// Hiển thị danh sách category trên sidebar(cột trái)
        /// </summary>
        /// <returns></returns>
        public ActionResult Sidebar()
        {
            using (var db = new WebDauGiaEntities())
            {
                var list = db.Categories.Where(c => c.Status == true).ToList();
                return PartialView("_SidebarCategoryPartial", list);
            }
        }
        /// <summary>
        /// Hiển thị danh sách category trên navbar
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            using (var db = new WebDauGiaEntities())
            {
                var list = db.Categories.Where(c => c.Status == true).ToList();
                return PartialView("_NavbarPartial", list);
            }
        }
    }
}