using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using System;

namespace WebDauGia.Models
{
    public class WinProductViewModel
    {
        public int ID { get; set; }
        public Nullable<int> VendorID { get; set; }
        public Nullable<int> WinnerID { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public string Des { get; set; }
        public Nullable<int> Price { get; set; }
        public Nullable<int> PriceAuction { get; set; }
        public Nullable<System.DateTime> DateEnd { get; set; }
    }
    public class HistoryAuctionProductVM
    {
        public Nullable<int> UserID { get; set; }
        public string UserName { get; set; }
        public Nullable<System.DateTime> DateAction { get; set; }
    }
    public class ProductFinishedDetailViewModel
    {
        public Nullable<int> Status { get; set; }
        public int ID { get; set; }
        public string Name { get; set; }
        public String VendorName { get; set; }
        public Nullable<int> Price { get; set; }
        public string Descrition { get; set; }
        public Nullable<System.DateTime> DateStart { get; set; }
        public Nullable<System.DateTime> DateEnd { get; set; }
        public Nullable<int> PriceAuction { get; set; }
        public int WinnerID { get; set; }
        public int VendorID { get; set; }
        public string WinnerName { get; set; }
        public Nullable<int> BuyNowPice { get; set; }
        public Nullable<int> FeedbackVendor { get; set; }
        public Nullable<int> FeedbackWinner { get; set; }

    }
    public class EditProductViewModel
    {
        public int ProductID { get; set; }

        public string Des { get; set; }
    }
    
    public class ProductHistoryViewModel
    {

        public int ID { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public string Des { get; set; }
        public Nullable<int> Price { get; set; }
        public Nullable<int> PriceAuction { get; set; }
        public Nullable<int> BuyNowPice { get; set; }
        public Nullable<System.DateTime> DateAuction { get; set; }
        public Nullable<System.DateTime> DateEnd { get; set; }
        public Nullable<int> WinnerID { get; set; }
        public Nullable<int> Denied { get; set; }
        public Nullable<int> Status { get; set; }
    }
    public class BuyItNowSuccessViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public Nullable<int> Price { get; set; }
        public Nullable<System.DateTime> DateStart { get; set; }

        public String VendorName { get; set; }
        public string Descrition { get; set; }
    }
    public class ProductDetailViewModel
    {
        public Nullable<int> Status { get; set; }
        public int ID { get; set; }
        public string Name { get; set; }
        public string VendorName { get; set; }
        public string VendorImage { get; set; }
        public Nullable<int> Price { get; set; }
        public string Description { get; set; }
        public Nullable<System.DateTime> DateStart { get; set; }
        public Nullable<System.DateTime> DateEnd { get; set; }
        public Nullable<int> PriceAuction { get; set; }
        public int WinnerID { get; set; }
        public int VendorID { get; set; }
        public string WinnerName { get; set; }
        public Nullable<int> StepPrice { get; set; }
        public Nullable<int> BuyNowPice { get; set; }
        public bool Watching { get; set; }
        public Nullable<int> FeedbackVendor { get; set; }
        public Nullable<int> FeedbackWinner { get; set; }

    }
    
    public class ProductHomeViewModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Winner { get; set; }
        public int WinnerID { get; set; }
        public string ImageUser { get; set; }
        public string CateName { get; set; }
        public int CateID { get; set; }
        public Nullable<System.DateTime> DateEnd { get; set; }
        public Nullable<System.DateTime> DateStart { get; set; }
        public Nullable<int> Price { get; set; }
        public Nullable<int> BuyItNow { get; set; }
        public string LinkImage { get; set; }
        public Nullable<int> VendorID { get; set; }
        public Nullable<int> Status { get; set; }
    }
}