using System.Security.Cryptography;
using System.Text;

namespace Codex.Utilities;

public static class EncryptionUtilities
{
    public static (string PublicKey, string PrivateKey) GenerateAsymmetricKeys()
    {
        using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
        {
            rsa.PersistKeyInCsp = false;
            var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
            var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());

            return (publicKey, privateKey);
        }
    }

    public static string DecryptWithPrivateKey(string textToDecrypt, string privateKeyBase64)
    {
        byte[] privateKeyBytes = Convert.FromBase64String(privateKeyBase64);
        using (RSA rsa = RSA.Create())
        {
            rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
            byte[] bytesToDecrypt = Convert.FromBase64String(textToDecrypt);
            byte[] decryptedBytes = rsa.Decrypt(bytesToDecrypt, RSAEncryptionPadding.Pkcs1);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }

    public static string EncryptWithPublicKey(string textToEncrypt, string publicKeyBase64)
    {
        byte[] publicKeyBytes = Convert.FromBase64String(publicKeyBase64);
        using (RSA rsa = RSA.Create())
        {
            rsa.ImportRSAPublicKey(publicKeyBytes, out _);
            byte[] bytesToEncrypt = Encoding.UTF8.GetBytes(textToEncrypt);
            byte[] encryptedBytes = rsa.Encrypt(bytesToEncrypt, RSAEncryptionPadding.Pkcs1);
            return Convert.ToBase64String(encryptedBytes);
        }
    }

    public static string Decrypt(string cipherText, string password, string name = null)
    {
        name ??= nameof(cipherText);

        try
        {
            var cipherBytes = Convert.FromBase64String(cipherText.Remove(startIndex: 0, count: 3));
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            using var memoryStream = new MemoryStream();
            using var cryptoStream = GetCryptoStream(memoryStream, passwordBytes, x => x.CreateDecryptor());
            cryptoStream.Write(cipherBytes, offset: 0, cipherBytes.Length);
            cryptoStream.Close();

            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
        catch
        {
            Contract.AssertFailure($"Could not decrypt '{name}' with provided password");
            return null;
        }
    }

    public static string Encrypt(string clearText, string password)
    {
        var clearBytes = Encoding.UTF8.GetBytes(clearText);
        var passwordBytes = Encoding.UTF8.GetBytes(password);

        using var memoryStream = new MemoryStream();
        using var cryptoStream = GetCryptoStream(memoryStream, passwordBytes, x => x.CreateEncryptor());
        cryptoStream.Write(clearBytes, offset: 0, clearBytes.Length);
        cryptoStream.Close();

        return $"v1:{Convert.ToBase64String(memoryStream.ToArray())}";
    }

    private static Stream GetCryptoStream(Stream stream, byte[] password, Func<SymmetricAlgorithm, ICryptoTransform> transformSelector)
    {
        var salt = new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 };
        var pdb = new Rfc2898DeriveBytes(password, salt, iterations: 10_000, HashAlgorithmName.SHA256);
        using var symmetricAlgorithm = Aes.Create();
        symmetricAlgorithm.Key = pdb.GetBytes(32);
        symmetricAlgorithm.IV = pdb.GetBytes(16);

        return new CryptoStream(stream, transformSelector(symmetricAlgorithm), CryptoStreamMode.Write);
    }

    public static string GetGeneratedPassword(int bits = 256)
    {
        var randomNumberGenerator = RandomNumberGenerator.Create();
        var password = new byte[bits / 8];
        randomNumberGenerator.GetBytes(password);
        return Convert.ToBase64String(password);
    }
}