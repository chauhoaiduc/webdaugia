using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebDauGia.Models
{
    public class UploadItemViewModel
    {
        public string Name { get; set; }
        public int CategoryID { get; set; }
        public int Price { get; set; }
        public int StepPrice { get; set; }
        public int BuyNowPrice { get; set; }
        public string Des { get; set; }
    }
    public class SettingViewModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Image { get; set; }
        public string Address { get; set; }
        public string NewPassword { get; set; }
        public string OldPassword { get; set; }
    }
    public class CommentUserViewModel
    {
        public int UserID { get; set; }
        public int ProductID { get; set; }
        public string Comment { get; set; }
        public int Point { get; set; }
    }
    public class CommentViewModel
    {
        public string Image { get; set; }
        public string Name { get; set; }
        public int Point { get; set; }
        public string Comment { get; set; }
    }
    public class ProfileViewModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public int Positive { get; set; }
        public int Negative { get; set; }
        public int Feedback { get; set; }
    }
    public class ComfirmEmailViewModel
    {
        public int UserID { get; set; }
        public string Code { get; set; }
    }
    public class UpdateWinnerViewModel
    {
        public Nullable<int> UserID { get; set; }
        public Nullable<int> Credit { get; set; }
        public Nullable<int> PriceAuction { get; set; }
    }
    public class FeedbackViewModel
    {
        public int ProductID { get; set; }
        public int WinnerID { get; set; }
        public int VendorID { get; set; }
        public string Comment { get; set; }
        public int Point { get; set; }
    }
    public class ChangePasswordViewModel
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }

    }
    public class LoginViewModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool Remember { get; set; }
    }

    public class RegisterViewModel
    {
        public string Username { get; set; }
        public string RawPassword { get; set; }

        public string Email { get; set; }
        public string FullName { get; set; }
        public string Address { get; set; }
    }

}
