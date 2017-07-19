using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Security.Cryptography;
using System.Text;
namespace WebDauGia.Helper
{
    public class StringUtils
    {
        public static string Md5(string strinput)
        {
            MD5 md5 = MD5CryptoServiceProvider.Create();
            byte[] input = Encoding.Default.GetBytes(strinput);
            byte[] output = md5.ComputeHash(input);
            string ret = BitConverter.ToString(output).Replace("-", "");
            return ret;
        }

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}