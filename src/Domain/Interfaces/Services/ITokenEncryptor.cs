namespace WeeklyUp.Domain.Interfaces.Services;

public interface ITokenEncryptor
{
    public string Encrypt(string plainText);
    public string Decrypt(string cipherText);
}
