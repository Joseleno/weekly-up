using FluentAssertions;
using Microsoft.Extensions.Options;
using WeeklyUp.Infrastructure.Security;

namespace WeeklyUp.Infrastructure.Tests.Security;

public sealed class AesTokenEncryptorTests
{
    private static readonly string ValidKey = Convert.ToBase64String(new byte[32]);

    private static AesTokenEncryptor CreateEncryptor(string? key = null) =>
        new(Options.Create(new AesEncryptionOptions
        {
            Key = key ?? ValidKey,
        }));

    [Fact]
    public void Encrypt_ShouldReturnBase64EncodedCipherText()
    {
        // Arrange
        var encryptor = CreateEncryptor();
        const string plainText = "my-secret-access-token";

        // Act
        var cipherText = encryptor.Encrypt(plainText);

        // Assert
        cipherText.Should().NotBeNullOrEmpty();

        var action = () => Convert.FromBase64String(cipherText);
        action.Should().NotThrow();
    }

    [Fact]
    public void Decrypt_AfterEncrypt_ShouldReturnOriginalText()
    {
        // Arrange
        var encryptor = CreateEncryptor();
        const string originalText = "original-token-value-roundtrip";

        // Act
        var cipherText = encryptor.Encrypt(originalText);
        var decrypted = encryptor.Decrypt(cipherText);

        // Assert
        decrypted.Should().Be(originalText);
    }

    [Fact]
    public void Encrypt_ShouldProduceDifferentCipherTextEachTime()
    {
        // Arrange
        var encryptor = CreateEncryptor();
        const string plainText = "same-plain-text";

        // Act
        var firstCipher = encryptor.Encrypt(plainText);
        var secondCipher = encryptor.Encrypt(plainText);

        // Assert
        firstCipher.Should().NotBe(secondCipher, "IV aleatorio garante ciphertexts diferentes");
    }

    [Fact]
    public void Decrypt_WithDifferentKey_ShouldThrowException()
    {
        // Arrange
        var encryptorA = CreateEncryptor();
        var differentKey = Convert.ToBase64String(new byte[32].Select((_, i) => (byte)(i + 1)).ToArray());
        var encryptorB = CreateEncryptor(differentKey);
        const string plainText = "some-access-token";

        var cipherText = encryptorA.Encrypt(plainText);

        // Act
        var act = () => encryptorB.Decrypt(cipherText);

        // Assert
        act.Should().Throw<Exception>();
    }
}
