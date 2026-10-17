using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Wickfall.Meta
{
    /// <summary>
    /// The encrypted save format (PDF §4 "Sauvegarde chiffrée", G25): JSON, AES-256-CBC with a fresh IV
    /// per save, and an HMAC-SHA256 over everything so tampering or corruption is detected.
    /// The keys are built in code: this deters casual editing, it is not real security.
    /// Layout: "WKF1" | IV (16) | HMAC (32) | ciphertext.
    /// </summary>
    public static class SaveCodec
    {
        static readonly byte[] Magic = { (byte)'W', (byte)'K', (byte)'F', (byte)'1' };
        const int IvLength = 16;
        const int MacLength = 32;

        static byte[] encryptionKey;
        static byte[] macKey;

        static void EnsureKeys()
        {
            if (encryptionKey != null)
                return;
            // Assembled from pieces of the game's own vocabulary so the string is not one literal.
            string seed = string.Concat("porte-meche", "|", "grand-foyer", "|", "cire=vie=lumiere=risque", "|", "wickfall");
            using (var sha = SHA256.Create())
            {
                encryptionKey = sha.ComputeHash(Encoding.UTF8.GetBytes(seed + "#enc"));
                macKey = sha.ComputeHash(Encoding.UTF8.GetBytes(seed + "#mac"));
            }
        }

        public static byte[] Encode(string json)
        {
            EnsureKeys();
            byte[] plain = Encoding.UTF8.GetBytes(json);
            byte[] iv = new byte[IvLength];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(iv);

            byte[] cipher;
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Key = encryptionKey;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using (var encryptor = aes.CreateEncryptor())
                    cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);
            }

            byte[] mac = ComputeMac(iv, cipher);
            using (var stream = new MemoryStream(Magic.Length + IvLength + MacLength + cipher.Length))
            {
                stream.Write(Magic, 0, Magic.Length);
                stream.Write(iv, 0, iv.Length);
                stream.Write(mac, 0, mac.Length);
                stream.Write(cipher, 0, cipher.Length);
                return stream.ToArray();
            }
        }

        /// <summary>Returns false (and null) for anything that is not an intact WICKFALL save.</summary>
        public static bool TryDecode(byte[] data, out string json)
        {
            json = null;
            EnsureKeys();
            int header = Magic.Length + IvLength + MacLength;
            if (data == null || data.Length <= header)
                return false;
            for (int i = 0; i < Magic.Length; i++)
                if (data[i] != Magic[i])
                    return false;

            byte[] iv = new byte[IvLength];
            byte[] mac = new byte[MacLength];
            byte[] cipher = new byte[data.Length - header];
            Buffer.BlockCopy(data, Magic.Length, iv, 0, IvLength);
            Buffer.BlockCopy(data, Magic.Length + IvLength, mac, 0, MacLength);
            Buffer.BlockCopy(data, header, cipher, 0, cipher.Length);

            if (!ConstantTimeEquals(mac, ComputeMac(iv, cipher)))
                return false;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.Key = encryptionKey;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    using (var decryptor = aes.CreateDecryptor())
                    {
                        byte[] plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                        json = Encoding.UTF8.GetString(plain);
                        return true;
                    }
                }
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        public static byte[] EncodeSave(SaveData data)
        {
            data.savedAtUtcTicks = DateTime.UtcNow.Ticks;
            return Encode(JsonUtility.ToJson(data));
        }

        public static bool TryDecodeSave(byte[] bytes, out SaveData data)
        {
            data = null;
            if (!TryDecode(bytes, out string json))
                return false;
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (ArgumentException)
            {
                return false;
            }
            if (data == null)
                return false;
            data.Sanitize();
            return true;
        }

        static byte[] ComputeMac(byte[] iv, byte[] cipher)
        {
            using (var hmac = new HMACSHA256(macKey))
            {
                hmac.TransformBlock(iv, 0, iv.Length, null, 0);
                hmac.TransformFinalBlock(cipher, 0, cipher.Length);
                return hmac.Hash;
            }
        }

        static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
