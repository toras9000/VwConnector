using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace VwConnector;

public record EncryptedData(EncryptionType Type, byte[] Data, byte[]? IV = default, byte[]? MAC = default)
{
    public static EncryptedData Parse(ReadOnlySpan<char> encryptedString)
        => EncryptedData.TryParse(encryptedString, out var key) ? key : throw new Exception("cannot parse encrypted key");

    public static bool TryParse(ReadOnlySpan<char> encryptedString, [NotNullWhen(true)] out EncryptedData? value)
    {
        value = default;
        if (encryptedString.IsWhiteSpace()) return false;
        var scan = encryptedString;
        var typeNum = scan.TakeSkipToken('.', out scan).TryParseNumber<int>();
        if (!typeNum.HasValue) return false;
        var type = (EncryptionType)typeNum.Value;
        if (!Enum.IsDefined(type)) return false;
        var part1 = scan.TakeSkipToken('|', out scan);
        var part2 = scan.TakeSkipToken('|', out scan);
        var part3 = scan.TakeSkipToken('|', out scan);

        static bool tryConstruct(EncryptionType type, ReadOnlySpan<char> data, ReadOnlySpan<char> iv, ReadOnlySpan<char> mac, [NotNullWhen(true)] out EncryptedData? value)
        {
            value = default;
            var dataBytes = data.DecodeBase64();
            if (dataBytes == null) return false;
            var ivBytes = iv.DecodeBase64();
            var macBytes = mac.DecodeBase64();
            value = new(type, Data: dataBytes, IV: ivBytes, MAC: macBytes);
            return true;
        }

        switch (type)
        {
        case EncryptionType.AesCbc256:
            return tryConstruct(type, data: part2, iv: part1, mac: null, out value);
        case EncryptionType.AesCbc128_HmacSha256:
        case EncryptionType.AesCbc256_HmacSha256:
            return tryConstruct(type, data: part2, iv: part1, mac: part3, out value);
        case EncryptionType.Rsa2048_OaepSha1:
        case EncryptionType.Rsa2048_OaepSha256:
            return tryConstruct(type, data: part1, iv: null, mac: null, out value);
        case EncryptionType.Rsa2048_OaepSha1_HmacSha256:
        case EncryptionType.Rsa2048_OaepSha256_HmacSha256:
            return tryConstruct(type, data: part1, iv: null, mac: part2, out value);
        default:
            return false;
        }
    }

    public string BuildString()
    {
        var builder = new StringBuilder();
        builder.Append((int)this.Type).Append('.');
        switch (this.Type)
        {
        case EncryptionType.AesCbc256:
            builder.Append(this.IV.EncodeBase64()).Append('|');
            builder.Append(this.Data.EncodeBase64());
            break;
        case EncryptionType.AesCbc128_HmacSha256:
        case EncryptionType.AesCbc256_HmacSha256:
            builder.Append(this.IV.EncodeBase64()).Append('|');
            builder.Append(this.Data.EncodeBase64()).Append('|');
            builder.Append(this.MAC.EncodeBase64());
            break;
        case EncryptionType.Rsa2048_OaepSha1:
        case EncryptionType.Rsa2048_OaepSha256:
            builder.Append(this.Data.EncodeBase64());
            break;
        case EncryptionType.Rsa2048_OaepSha1_HmacSha256:
        case EncryptionType.Rsa2048_OaepSha256_HmacSha256:
            builder.Append(this.Data.EncodeBase64()).Append('|');
            builder.Append(this.MAC.EncodeBase64());
            break;
        default:
            throw new NotImplementedException();
        }
        return builder.ToString();
    }
}

