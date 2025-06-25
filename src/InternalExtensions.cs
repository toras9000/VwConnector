using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace VwConnector;

internal static class InternalExtensions
{
    public static ReadOnlySpan<char> TakeSkipToken(this ReadOnlySpan<char> self, char separator, out ReadOnlySpan<char> next)
    {
        var pos = self.IndexOf(separator);
        if (pos < 0)
        {
            next = self[self.Length..];
            return self;
        }

        next = self[(pos + 1)..];
        return self[..pos];
    }

    public static byte[]? DecodeBase64(this ReadOnlySpan<char> self)
    {
        var maxlen = Base64.GetMaxDecodedFromUtf8Length(self.Length);
        var buffer = ArrayPool<byte>.Shared.Rent(maxlen);
        try
        {
            var result = Convert.TryFromBase64Chars(self, buffer, out var actlen);
            var bytes = result ? buffer[..actlen] : default;
            return bytes;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    [return: NotNullIfNotNull(nameof(self))]
    public static string? EncodeBase64(this byte[]? self)
        => self == null ? default : Convert.ToBase64String(self);

    [return: NotNullIfNotNull(nameof(self))]
    public static byte[]? EncodeUtf8(this string? self)
        => self == null ? default : Encoding.UTF8.GetBytes(self);

    public static TNumber? TryParseNumber<TNumber>(this ReadOnlySpan<char> self) where TNumber : struct, INumber<TNumber>
        => TNumber.TryParse(self, CultureInfo.InvariantCulture, out var result) ? result : default(TNumber?);


}
