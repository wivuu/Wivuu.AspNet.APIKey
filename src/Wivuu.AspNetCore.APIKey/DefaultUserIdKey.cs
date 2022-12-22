
using System.Text;

namespace Wivuu.AspNetCore.APIKey;

public readonly record struct DefaultUserIdKey<T>(T UserId) : IDataProtectedKey
    where T : IParsable<T>
{
    public static bool TryParseTokenBytes(byte[] tokenBytes, out IDataProtectedKey? key)
    {
        var tokenString = Encoding.UTF8.GetString(tokenBytes);

        if (T.TryParse(tokenString, default, out var userId))
        {
            key = new DefaultUserIdKey<T>(userId);
            return true;
        }
        else
        {
            key = null;
            return false;
        }
    }

    public byte[] ToTokenBytes()
    {
        var idString = UserId.ToString() ?? throw new NullReferenceException("userId must not be null");
        return Encoding.UTF8.GetBytes(idString);
    }
}