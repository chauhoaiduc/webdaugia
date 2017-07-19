using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Web;

namespace WebDauGia.Helper
{
    public class Email
    {
        // Cấu hình email
        public static void Config(SmtpClient smtp)
        {
            smtp.Host = "smtp.gmail.com";
            smtp.Port = 587;
            smtp.UseDefaultCredentials = true;
            smtp.Credentials = new System.Net.NetworkCredential("jackhard006@gmail.com", "maszero11");// tài khoản Gmail
            smtp.EnableSsl = true;
        }

        // Phuong thuc gui
        public static void Send(string mailto, string subject, StringBuilder body, LinkedResource res)
        {
            AlternateView altView = AlternateView.CreateAlternateViewFromString(body.ToString(), null, MediaTypeNames.Text.Html);
            altView.LinkedResources.Add(res);
            MailMessage mail = new MailMessage();
            mail.To.Add(mailto);
            mail.From = new MailAddress("jackhard006@gmail.com");
            mail.Subject = subject;
            mail.Body = body.ToString();
            mail.AlternateViews.Add(altView);
            mail.IsBodyHtml = true;
            SmtpClient smtp = new SmtpClient();
            Config(smtp);
            smtp.Send(mail);
        }

        public static void Send(string mailto, string subject, StringBuilder body)
        {
            MailMessage mail = new MailMessage();
            mail.To.Add(mailto);
            mail.From = new MailAddress("jackhard006@gmail.com");
            mail.Subject = subject;
            mail.Body = body.ToString();
            mail.IsBodyHtml = true;
            SmtpClient smtp = new SmtpClient();
            Config(smtp);
            smtp.Send(mail);
        }
    }
}