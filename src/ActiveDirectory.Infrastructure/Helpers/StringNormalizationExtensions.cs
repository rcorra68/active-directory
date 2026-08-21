namespace ActiveDirectory.Infrastructure.Helpers;

public static class StringNormalizationExtensions
{
    /// <summary>
    /// Removes whitespace, non-printable control characters, and normalizes casing for dictionary keys.
    /// </summary>
    public static string NormalizeCadastralCode(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        Span<char> buffer = stackalloc char[input.Length];
        int idx = 0;

        foreach (char c in input)
        {
            if (!char.IsWhiteSpace(c) && !char.IsControl(c))
            {
                buffer[idx++] = char.ToUpperInvariant(c);
            }
        }

        return new string(buffer[..idx]);
    }
}