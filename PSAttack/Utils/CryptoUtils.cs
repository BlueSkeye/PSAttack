using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PSAttack.Utils
{
    internal static class CryptoUtils
    {
        public static MemoryStream Decrypt(this string text, string password)
        {
            byte[] data = Convert.FromBase64String(text.Replace("_", "/"));
            using (Stream stream = new MemoryStream(data)) {
                return stream.Decrypt(password);
            }
        }

        public static MemoryStream Decrypt(this Stream inputStream, string password)
        {
            try {
                byte[] keyBytes = Encoding.Unicode.GetBytes(password);

                Rfc2898DeriveBytes derivedKey = new Rfc2898DeriveBytes(password, keyBytes);
                RijndaelManaged rijndaelCSP = new RijndaelManaged();
                rijndaelCSP.Key = derivedKey.GetBytes(rijndaelCSP.KeySize / 8);
                rijndaelCSP.IV = derivedKey.GetBytes(rijndaelCSP.BlockSize / 8);
                ICryptoTransform decryptor = rijndaelCSP.CreateDecryptor();

                using (CryptoStream decryptStream =
                    new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read))
                {
                    byte[] inputFileData = new byte[(int)inputStream.Length];
                    try
                    {
                        return new MemoryStream(
                            Encoding.Unicode.GetBytes(
                                new StreamReader(decryptStream).ReadToEnd()));
                    }
                    finally { rijndaelCSP.Clear(); }
                }
            }
            finally { inputStream.Close();}
        }
    }
}
