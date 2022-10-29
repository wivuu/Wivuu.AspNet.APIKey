namespace Wivuu.AspNetCore.APIKey;

public struct DefaultAPIKey : IDataProtectionKey
{
    static readonly byte[] BytePayload = new byte[] { 0x01 };

    public static bool TryParseTokenBytes(byte[] tokenBytes, out IDataProtectionKey? key)
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