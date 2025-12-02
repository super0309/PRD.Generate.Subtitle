using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace VideoSubtitleGenerator.Core
{
    public static class Utilities

    {

        /// <summary>
        /// Declare the event  LogUpdatedEvent
        /// </summary>
        public delegate void LogUpdatedEventHandler();
        public static event LogUpdatedEventHandler LogUpdatedEvent;

        /// <summary>
        /// Write log errror
        /// </summary>
        /// <param name="exc"></param>
        public static void WriteToLog(Exception exc)
        {
            if (!exc.Message.Contains("The DELETE statement conflicted"))
            {
                ErrorLog.ErrorRoutine(false, exc);
                RaiseLogUpdatedEvent();
            }
        }

        /// <summary>
        /// RaiseLogUpdatedEvent
        /// </summary>
        private static void RaiseLogUpdatedEvent()
        {
            if (LogUpdatedEvent != null)
            {
                LogUpdatedEvent();
            }
        }

        /// <summary>
        /// Write log information
        /// </summary>
        /// <param name="strMessage"></param>
        public static void WriteLogInfor(string strMessage)
        {
            if (!string.IsNullOrEmpty(strMessage))
            {
                ErrorLog.ErrorRoutinLogInfor(strMessage);
                RaiseLogUpdatedEvent();
            }
        }

        public static string EncryptToUrlToken(string plaintext, string secretKey)
        {
            try
            {
                byte[] salt = RandomNumberGenerator.GetBytes(16);   // cho PBKDF2
                byte[] key = DeriveKey(secretKey, salt, 32);       // 256-bit
                byte[] nonce = RandomNumberGenerator.GetBytes(12);  // AesGcm yêu cầu 12 bytes

                byte[] plainBytes = Encoding.UTF8.GetBytes(plaintext);
                byte[] cipher = new byte[plainBytes.Length];
                byte[] tag = new byte[16];

                using (var aesgcm = new AesGcm(key))
                {
                    aesgcm.Encrypt(nonce, plainBytes, cipher, tag);
                }

                // Gói lại: [1 byte version][16 salt][12 nonce][N cipher][16 tag]
                byte version = 1;
                byte[] token = new byte[1 + salt.Length + nonce.Length + cipher.Length + tag.Length];
                token[0] = version;
                Buffer.BlockCopy(salt, 0, token, 1, salt.Length);
                Buffer.BlockCopy(nonce, 0, token, 1 + 16, nonce.Length);
                Buffer.BlockCopy(cipher, 0, token, 1 + 16 + 12, cipher.Length);
                Buffer.BlockCopy(tag, 0, token, 1 + 16 + 12 + cipher.Length, tag.Length);

                return Base64UrlEncode(token);
            }
            catch (Exception ex)
            { 
                Utilities.WriteToLog(ex);
                return string.Empty;
            }   
        }

        // Decrypt từ base64url token
        public static string DecryptFromUrlToken(string tokenBase64Url, string secretKey)
        {
            try
            {
                byte[] token = Base64UrlDecode(tokenBase64Url); 
                if(token == null) throw new ArgumentException("Token không hợp lệ.");

                if (token.Length < 1 + 16 + 12 + 16) // min length check
                    throw new ArgumentException("Token không hợp lệ.");

                byte version = token[0];
                if (version != 1) throw new NotSupportedException("Phiên bản token không hỗ trợ.");

                // Tách thành phần
                byte[] salt = new byte[16];
                byte[] nonce = new byte[12];
                Buffer.BlockCopy(token, 1, salt, 0, 16);
                Buffer.BlockCopy(token, 1 + 16, nonce, 0, 12);

                int cipherLen = token.Length - (1 + 16 + 12 + 16);
                byte[] cipher = new byte[cipherLen];
                byte[] tag = new byte[16];

                Buffer.BlockCopy(token, 1 + 16 + 12, cipher, 0, cipherLen);
                Buffer.BlockCopy(token, 1 + 16 + 12 + cipherLen, tag, 0, 16);

                byte[] key = DeriveKey(secretKey, salt, 32);

                byte[] plain = new byte[cipher.Length];
                using (var aesgcm = new AesGcm(key))
                {
                    aesgcm.Decrypt(nonce, cipher, tag, plain);
                }
                return Encoding.UTF8.GetString(plain);
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                return string.Empty;
            }   
        }

        // -------- Helpers --------
        private static byte[] DeriveKey(string secret, byte[] salt, int size)
        {
            try
            {
                // PBKDF2-HMACSHA256, 100k vòng (cân bằng giữa bảo mật và hiệu năng)
                using var pbkdf2 = new Rfc2898DeriveBytes(secret, salt, 100_000, HashAlgorithmName.SHA256);
                return pbkdf2.GetBytes(size);
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                return null;
            }
            
        }

        // Base64Url (không có '=' và URL-safe)
        private static string Base64UrlEncode(byte[] data)
        {
            try
            {
                string b64 = Convert.ToBase64String(data);
                return b64.Replace("+", "-").Replace("/", "_").TrimEnd('=');
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                return null;
            }
            
        }

        private static byte[] Base64UrlDecode(string s)
        {
            try
            {
                string b64 = s.Replace("-", "+").Replace("_", "/");
                switch (b64.Length % 4)
                {
                    case 2: b64 += "=="; break;
                    case 3: b64 += "="; break;
                }
                return Convert.FromBase64String(b64);
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                return null;
            }
        }


        /// <summary>
        /// UTF8SHA256
        /// </summary>
        /// <param name="randomString"></param>
        /// <returns></returns>
        public static string UTF8SHA256(string randomString)
        {
            try
            {
                var crypt = new System.Security.Cryptography.SHA256Managed();
                var hash = new System.Text.StringBuilder();
                byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(randomString));
                foreach (byte theByte in crypto) hash.Append(theByte.ToString("x2"));
                return hash.ToString();
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// ToBase64
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string ToBase64(string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Deserialize
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xmlString"></param>
        /// <returns></returns>
        public static T Deserialize<T>(string xmlString)
        {
            if (xmlString == null) return default;
            var serializer = new XmlSerializer(typeof(T));
            using (var reader = new StringReader(xmlString))
            {
                return (T)serializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// ReadFile
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string ReadFile(string path)
        {
            try
            {
                var bytes = File.ReadAllBytes(path);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                return string.Empty;
            }
        }

        public static List<long?> ConvertToListLongs(List<string> lst)
        {
            return lst.Select(s => Int64.TryParse(s, out long n) ? n : (long?)null).ToList();
        }

        /// <summary>
        /// CheckLstStrToLong
        /// </summary>
        /// <param name="lst"></param>
        /// <returns></returns>
        public static bool CheckLstStrToLong(List<string> lst)
        {
            try
            {
                var items = lst.Select(s => Int64.TryParse(s, out long n) ? n : (long?)null).ToList();
                if (items != null && items.Any()) return true;
                else return false;
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                return false;
            }
        }

        public static IEnumerable<long> ConvertIdFres(IEnumerable<long> input, long count )
        {
            var itemOutput = new List<long>();
            input.OrderBy(m => m).ToList();
            try
            {
                int index = 1;
                foreach (var item in input)
                {
                    itemOutput.Add(index++);
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
            return itemOutput;
        }



    }
}
