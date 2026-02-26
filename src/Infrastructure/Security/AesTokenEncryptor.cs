using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;

using WeeklyUp.Domain.Interfaces.Services;

namespace WeeklyUp.Infrastructure.Security;

public sealed class AesTokenEncryptor : ITokenEncryptor
{
    private readonly byte[] _key;

    public AesTokenEncryptor(IOptions<AesEncryptionOptions> options)
    {
        _key = Convert.FromBase64String(options.Value.Key);
    }

    public string Encrypt(string plainText)
    {
        using var aes = CreateAes();
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        aes.IV.CopyTo(result, 0);
        cipherBytes.CopyTo(result, aes.IV.Length);
        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        var allBytes = Convert.FromBase64String(cipherText);
        using var aes = CreateAes();
        aes.IV = allBytes[..16];
        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(allBytes, 16, allBytes.Length - 16);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private Aes CreateAes()
    {
        var aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        return aes;
    }
}
