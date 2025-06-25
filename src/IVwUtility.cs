using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace VwConnector;

public interface IVwUtility : IVwScope
{
    public byte[] GenerateKeyData(bool simple = false)
    {
        if (simple)
        {
            var buffer = new byte[64];
            Random.Shared.NextBytes(buffer);
            return buffer;
        }

        // Imitation of web vault implementation.
        var key = new byte[64];
        using var aes = Aes.Create();
        aes.KeySize = 32 * 8;   // bits
        aes.GenerateKey();
        aes.Key.CopyTo(key.AsSpan(0, 32));
        aes.GenerateKey();
        aes.Key.CopyTo(key.AsSpan(32));
        return key;
    }

    public RsaKeyPair GenerateRsaKeyPair()
    {
        using var rsa = RSA.Create();
        var publicKey = rsa.ExportSubjectPublicKeyInfo();
        var privateKey = rsa.ExportPkcs8PrivateKey();
        return new(publicKey, privateKey);
    }

    public byte[] CreatePasswordHash(string email, string password, KdfConfig config)
    {
        if (config.kdf != KdfType.Pbkdf2) throw new NotSupportedException();
        var passBytes = Encoding.UTF8.GetBytes(password);
        var mailBytes = Encoding.UTF8.GetBytes(email);
        var masterKey = Rfc2898DeriveBytes.Pbkdf2(passBytes, mailBytes, config.kdfIterations, HashAlgorithmName.SHA256, 32);
        var passHash = Rfc2898DeriveBytes.Pbkdf2(masterKey, passBytes, 1, HashAlgorithmName.SHA256, 32);
        return passHash;
    }

    public byte[] CreateMasterKey(string email, string password, KdfConfig config)
    {
        if (config.kdf != KdfType.Pbkdf2) throw new NotSupportedException();
        var passBytes = Encoding.UTF8.GetBytes(password);
        var mailBytes = Encoding.UTF8.GetBytes(email);
        var masterKey = Rfc2898DeriveBytes.Pbkdf2(passBytes, mailBytes, config.kdfIterations, HashAlgorithmName.SHA256, 32);
        return masterKey;
    }

    public byte[] CreateExpandKey(byte[] key, string info)
    {
        var writer = new ArrayBufferWriter<byte>(info.Length + 1);
        Encoding.UTF8.GetBytes(info, writer);
        writer.Write<byte>([1]);
        return HMACSHA256.HashData(key, writer.WrittenSpan.ToArray());
    }

    public SymmetricCryptoKey CreateStretchKey(string email, string password, KdfConfig config)
    {
        var masterKey = this.CreateMasterKey(email, password, config);
        var stretchKey = this.CreateStretchKey(masterKey);
        return stretchKey;
    }

    public SymmetricCryptoKey CreateStretchKey(byte[] key)
    {
        var encKey = CreateExpandKey(key, "enc");
        var macKey = CreateExpandKey(key, "mac");
        var stretchKey = new SymmetricCryptoKey(EncryptionType.AesCbc256_HmacSha256, encKey, macKey);
        return stretchKey;
    }

    public byte[] EncryptPublicKey(byte[] publicKey, byte[] data)
    {
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(publicKey, out _);
        var encrypted = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA1);
        return encrypted;
    }

    public byte[] DecryptPrivateKey(byte[] privateKey, byte[] data)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(privateKey, out _);
        var decrypted = rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA1);
        return decrypted;
    }

    public EncryptedData EncryptAes(SymmetricCryptoKey key, byte[] data, bool hmac, byte[]? iv = default)
    {
        static byte[] makeMac(byte[] key, byte[] iv, byte[] data)
        {
            byte[] macData = [.. iv, .. data];
            var mac = HMACSHA256.HashData(key, macData);
            return mac;
        }

        using var aes = Aes.Create();
        aes.Key = key.EncKey;
        if (iv == null) aes.GenerateIV(); else aes.IV = iv;
        var enc = aes.EncryptCbc(data, aes.IV);
        if (hmac)
        {
            var type = key.EncKey.Length switch { 16 => EncryptionType.AesCbc128_HmacSha256, 32 => EncryptionType.AesCbc256_HmacSha256, _ => throw new NotSupportedException(), };
            var mac = makeMac(key.AuthKey ?? [], aes.IV, enc);
            return new(type, enc, aes.IV, mac);
        }
        else
        {
            var type = key.EncKey.Length switch { 64 => EncryptionType.AesCbc256, _ => throw new NotSupportedException(), };
            return new(type, enc, aes.IV);
        }
    }

    public EncryptedData EncryptRsa(byte[] key, byte[] data, bool sha256 = true, byte[]? iv = default)
    {
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(key, out _);
        var (type, padding) = sha256 ? (EncryptionType.Rsa2048_OaepSha256, RSAEncryptionPadding.OaepSHA256) : (EncryptionType.Rsa2048_OaepSha1, RSAEncryptionPadding.OaepSHA1);
        var enc = rsa.Encrypt(data, padding);
        return new(type, enc);
    }

    public byte[] Decrypt(byte[] key, EncryptedData encripted)
    {
        switch (encripted.Type)
        {
        case EncryptionType.AesCbc256:
        case EncryptionType.AesCbc128_HmacSha256:
        case EncryptionType.AesCbc256_HmacSha256:
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                return aes.DecryptCbc(encripted.Data, encripted.IV!);
            }
        case EncryptionType.Rsa2048_OaepSha1:
        case EncryptionType.Rsa2048_OaepSha1_HmacSha256:
            using (var rsa = RSA.Create())
            {
                rsa.ImportPkcs8PrivateKey(key, out _);
                return rsa.Decrypt(encripted.Data, RSAEncryptionPadding.OaepSHA1);
            }
        case EncryptionType.Rsa2048_OaepSha256:
        case EncryptionType.Rsa2048_OaepSha256_HmacSha256:
            using (var rsa = RSA.Create())
            {
                rsa.ImportPkcs8PrivateKey(key, out _);
                return rsa.Decrypt(encripted.Data, RSAEncryptionPadding.OaepSHA256);
            }
        default:
            throw new InvalidDataException();
        }
    }
}
