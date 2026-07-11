namespace BillingSystem.Helpers;

/// <summary>
/// Converts numeric amounts to Indian English words.
/// Used for the "Amount in Words" field on GST invoices.
///
/// Uses Indian number system (Lakh, Crore) as required for Indian businesses.
///
/// Examples:
///   1250.50  → "One Thousand Two Hundred Fifty Rupees And Fifty Paise Only"
///   6300.00  → "Six Thousand Three Hundred Rupees Only"
///   100000   → "One Lakh Rupees Only"
///   10000000 → "One Crore Rupees Only"
/// </summary>
public static class NumberToWords
{
    private static readonly string[] Ones =
    {
        "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
        "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen",
        "Sixteen", "Seventeen", "Eighteen", "Nineteen"
    };

    private static readonly string[] Tens =
    {
        "", "", "Twenty", "Thirty", "Forty", "Fifty",
        "Sixty", "Seventy", "Eighty", "Ninety"
    };

    /// <summary>
    /// Converts a decimal rupee amount to words.
    /// </summary>
    /// <param name="amount">Amount in rupees (e.g. 6300.50)</param>
    /// <returns>Human-readable string (e.g. "Six Thousand Three Hundred Rupees And Fifty Paise Only")</returns>
    public static string Convert(decimal amount)
    {
        if (amount == 0) return "Zero Rupees Only";

        amount = Math.Round(amount, 2);

        long rupees = (long)Math.Floor(amount);
        int paise   = (int)Math.Round((amount - rupees) * 100);

        var result = ConvertLong(rupees) + " Rupees";

        if (paise > 0)
            result += " And " + ConvertInt(paise) + " Paise";

        return result + " Only";
    }

    private static string ConvertLong(long number)
    {
        if (number == 0) return "Zero";
        if (number < 0)  return "Minus " + ConvertLong(-number);

        var parts = new List<string>();

        // Crore (10,000,000)
        if (number >= 10_000_000)
        {
            parts.Add(ConvertInt((int)(number / 10_000_000)) + " Crore");
            number %= 10_000_000;
        }

        // Lakh (100,000)
        if (number >= 100_000)
        {
            parts.Add(ConvertInt((int)(number / 100_000)) + " Lakh");
            number %= 100_000;
        }

        // Thousand
        if (number >= 1_000)
        {
            parts.Add(ConvertInt((int)(number / 1_000)) + " Thousand");
            number %= 1_000;
        }

        // Remaining (hundreds, tens, ones)
        if (number > 0)
            parts.Add(ConvertInt((int)number));

        return string.Join(" ", parts);
    }

    private static string ConvertInt(int number)
    {
        if (number == 0)  return string.Empty;
        if (number < 20)  return Ones[number];
        if (number < 100) return Tens[number / 10] + (number % 10 > 0 ? " " + Ones[number % 10] : "");

        // 100–999
        string hundreds = Ones[number / 100] + " Hundred";
        int remainder   = number % 100;
        return remainder == 0 ? hundreds : hundreds + " " + ConvertInt(remainder);
    }
}
