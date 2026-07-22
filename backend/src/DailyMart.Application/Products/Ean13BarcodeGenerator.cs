using System.Security.Cryptography;
using System.Text;

namespace DailyMart.Application.Products;

/// <summary>
/// Generates a valid EAN-13 barcode value, prefixed "20" - GS1's reserved "in-store use" range, exactly
/// this scenario (an internally-assigned barcode with no real GS1 registration). Used only when a
/// product is created without an explicit barcode - see ProductService.
/// </summary>
internal static class Ean13BarcodeGenerator
{
    public static string Generate()
    {
        var digits = new int[12];
        digits[0] = 2;
        digits[1] = 0;

        for (var i = 2; i < 12; i++)
        {
            digits[i] = RandomNumberGenerator.GetInt32(0, 10);
        }

        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            sum += digits[i] * (i % 2 == 0 ? 1 : 3);
        }

        var checkDigit = (10 - (sum % 10)) % 10;

        var builder = new StringBuilder(13);
        foreach (var digit in digits)
        {
            builder.Append(digit);
        }
        builder.Append(checkDigit);

        return builder.ToString();
    }
}
