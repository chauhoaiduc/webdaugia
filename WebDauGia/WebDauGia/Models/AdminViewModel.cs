using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebDauGia.Models
{
    public class ManageUserViewModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Image { get; set; }
        public Nullable<int> Credits { get; set; }
        public string Address { get; set; }
        public string UserName { get; set; }
        public Nullable<int> Positive { get; set; }
        public Nullable<int> Negative { get; set; }
    }
    public class ProductApproveViewModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Des { get; set; }
        public string Image { get; set; }
        public String VendorName { get; set; }
        public Nullable<int> VendorID { get; set; }
        public Nullable<int> Price { get; set; }
        public Nullable<int> StepPrice { get; set; }
        public Nullable<int> BuyNowPice { get; set; }
        public Nullable<System.DateTime> DateStart { get; set; }
        public Nullable<bool> AutoExtend { get; set; }

    }
}