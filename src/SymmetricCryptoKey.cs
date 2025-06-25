namespace VwConnector;

public record SymmetricCryptoKey(EncryptionType Type, byte[] EncKey, byte[]? AuthKey)
{
    public static SymmetricCryptoKey From(byte[] data)
    {
        if (data.Length == 32) return new(EncryptionType.AesCbc256, data, null);
        if (data.Length == 64) return new(EncryptionType.AesCbc256_HmacSha256, data[..32], data[32..]);
        throw new InvalidDataException();
    }

    public byte[] ToBytes() => [.. this.EncKey, .. this.AuthKey ?? []];
}