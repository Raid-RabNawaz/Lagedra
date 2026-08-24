namespace Lagedra.SharedKernel.Sms;

/// <summary>
/// Normalizes user-entered phone numbers to E.164. Twilio requires E.164 and
/// interprets anything else at its own discretion — production had numbers
/// stored as "7149105177" and "(818) 305-6520" flowing straight into the
/// Messaging API.
/// </summary>
public static class PhoneNumberE164
{
    private const int MinDigits = 8;
    private const int MaxDigits = 15; // E.164 maximum

    /// <summary>
    /// Attempts to normalize <paramref name="input"/> to E.164 (+15551234567).
    /// Accepts international numbers prefixed with '+' and US national
    /// formats (10 digits, or 11 digits starting with 1) with common
    /// separators — spaces, dashes, dots, parentheses.
    /// </summary>
    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var trimmed = input.Trim();
        var hasPlus = trimmed[0] == '+';

        Span<char> digits = stackalloc char[MaxDigits + 1];
        var count = 0;

        foreach (var ch in hasPlus ? trimmed.AsSpan(1) : trimmed.AsSpan())
        {
            if (char.IsAsciiDigit(ch))
            {
                if (count >= MaxDigits)
                {
                    return false;
                }

                digits[count++] = ch;
                continue;
            }

            if (ch is ' ' or '-' or '.' or '(' or ')')
            {
                continue;
            }

            return false;
        }

        if (hasPlus)
        {
            if (count < MinDigits || digits[0] == '0')
            {
                return false;
            }

            normalized = string.Concat("+", new string(digits[..count]));
            return true;
        }

        // National input: the platform is US-focused, so bare 10-digit
        // numbers (and 11 digits with a leading 1) are treated as +1.
        if (count == 10 && digits[0] is not ('0' or '1'))
        {
            normalized = string.Concat("+1", new string(digits[..count]));
            return true;
        }

        if (count == 11 && digits[0] == '1' && digits[1] is not ('0' or '1'))
        {
            normalized = string.Concat("+", new string(digits[..count]));
            return true;
        }

        return false;
    }
}
