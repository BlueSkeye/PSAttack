using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PSAttack.Utils
{
    internal static class CryptoUtils
    {
        public static string DecryptString(string text)
        {
            string key = Properties.Settings.Default.encryptionKey;
            Rfc2898DeriveBytes derivedKey =
                new Rfc2898DeriveBytes(key, Encoding.Unicode.GetBytes(key));
            RijndaelManaged rijndaelCSP = new RijndaelManaged();
            rijndaelCSP.Key = derivedKey.GetBytes(rijndaelCSP.KeySize / 8);
            rijndaelCSP.IV = derivedKey.GetBytes(rijndaelCSP.BlockSize / 8);
            using (ICryptoTransform decryptor = rijndaelCSP.CreateDecryptor()) {
                byte[] inputbuffer = Convert.FromBase64String(text.Replace("_", "/"));
                return Encoding.Unicode.GetString(
                    decryptor.TransformFinalBlock(inputbuffer, 0, inputbuffer.Length));
            }
        }

        public static MemoryStream DecryptFile(Stream inputStream)
        {
            try {
                string key = Properties.Settings.Default.encryptionKey;
                byte[] keyBytes = Encoding.Unicode.GetBytes(key);

                Rfc2898DeriveBytes derivedKey = new Rfc2898DeriveBytes(key, keyBytes);
                RijndaelManaged rijndaelCSP = new RijndaelManaged();
                rijndaelCSP.Key = derivedKey.GetBytes(rijndaelCSP.KeySize / 8);
                rijndaelCSP.IV = derivedKey.GetBytes(rijndaelCSP.BlockSize / 8);
                ICryptoTransform decryptor = rijndaelCSP.CreateDecryptor();

                using (CryptoStream decryptStream = new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read)) {
                    byte[] inputFileData = new byte[(int)inputStream.Length];
                    try {
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
