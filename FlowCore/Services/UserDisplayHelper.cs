namespace FlowCore.Services;

public static class UserDisplayHelper
{
    private static readonly string[] AvatarPalette =
    [
        "#4f46e5", "#7c3aed", "#db2777", "#dc2626", "#ea580c", "#ca8a04", "#16a34a", "#0891b2"
    ];

    public static string GetInitials(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "?";

        foreach (var ch in fullName.Trim())
        {
            if (char.IsLetter(ch))
                return char.ToUpperInvariant(ch).ToString();
        }

        return "?";
    }

    public static string BackgroundColorForUser(Guid userId)
    {
        var bytes = userId.ToByteArray();
        var idx = (bytes[0] ^ bytes[^1] ^ bytes[7]) % AvatarPalette.Length;
        if (idx < 0)
            idx += AvatarPalette.Length;
        return AvatarPalette[idx];
    }
}
