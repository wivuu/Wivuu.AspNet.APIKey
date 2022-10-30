namespace Wivuu.AspNetCore.APIKey;

public struct DefaultAPIKey : IDataProtectedKey
{
    static readonly byte[] BytePayload = new byte[] { 0x01 };

    public static bool TryParseTokenBytes(byte[] tokenBytes, out IDataProtectedKey? key)
    {
        if (tokenBytes[0] == 0x01)
        {
            key = new DefaultAPIKey();
            return true;
        }
        else
        {
            key = null;
            return false;
        }
    }

    public byte[] ToTokenBytes() => BytePayload;
}