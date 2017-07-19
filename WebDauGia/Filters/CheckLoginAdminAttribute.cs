using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebDauGia.Helper;

namespace WebDauGia.Filters
{
    public class CheckLoginAdminAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (CurrentContext.IsLogged() == false)
            {
                filterContext.Result = new RedirectResult("~/Account/Login");
            }
            else
            {
                if (CurrentContext.GetUser().Role != 1)
                {
                    filterContext.Result = new RedirectResult("~/");
                }

            }
            base.OnActionExecuting(filterContext);
        }
    }
}