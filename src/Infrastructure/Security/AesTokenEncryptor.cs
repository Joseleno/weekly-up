using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;

using WeeklyUp.Domain.Interfaces.Services;

namespace WeeklyUp.Infrastructure.Security;

public sealed class AesTokenEncryptor : ITokenEncryptor
{
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private readonly byte[] _keyBytes;

    public AesTokenEncryptor(IOptions<AesEncryptionOptions> options)
    {
        _keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(options.Value.Key));
    }

    public string Encrypt(string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var nonce = new byte[NonceSizeBytes];
        RandomNumberGenerator.Fill(nonce);
        var cipherText = new byte[plainBytes.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(_keyBytes, TagSizeBytes);
        aes.Encrypt(nonce, plainBytes, cipherText, tag);

        var result = new byte[NonceSizeBytes + cipherText.Length + TagSizeBytes];
        nonce.CopyTo(result, 0);
        cipherText.CopyTo(result, NonceSizeBytes);
        tag.CopyTo(result, NonceSizeBytes + cipherText.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherText);

        var data = Convert.FromBase64String(cipherText);
        var nonce = data[..NonceSizeBytes];
        var tag = data[^TagSizeBytes..];
        var cipher = data[NonceSizeBytes..^TagSizeBytes];
        var plainText = new byte[cipher.Length];

        using var aes = new AesGcm(_keyBytes, TagSizeBytes);
        aes.Decrypt(nonce, cipher, tag, plainText);

        return Encoding.UTF8.GetString(plainText);
    }
}
