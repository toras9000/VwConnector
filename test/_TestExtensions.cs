namespace VwConnector.Tests;

public static class RandomExtensions
{
    public static byte[] GetBytes(this Random self, int length)
    {
        var buffer = new byte[length];
        self.NextBytes(buffer);
        return buffer;
    }

}
