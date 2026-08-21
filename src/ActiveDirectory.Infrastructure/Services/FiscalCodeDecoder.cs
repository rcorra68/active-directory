using ActiveDirectory.Core.Interfaces;
using ActiveDirectory.Infrastructure.Helpers;

namespace ActiveDirectory.Infrastructure.Services;

/// <summary>
/// Decodes Italian Fiscal Codes (Codice Fiscale) and resolves birth places using cadastral codes.
/// </summary>
public class FiscalCodeDecoder : IFiscalCodeDecoder
{
    private const string MonthCodes = "ABCDEHLMPRST";
    private readonly IReadOnlyDictionary<string, string> _cadastralDictionary;

    public FiscalCodeDecoder(IReadOnlyDictionary<string, string> cadastralDictionary)
    {
        _cadastralDictionary = cadastralDictionary ?? new Dictionary<string, string>();
    }

    public FiscalCodeInfo? Decode(string fiscalCode)
    {
        if (string.IsNullOrWhiteSpace(fiscalCode) || fiscalCode.Length != 16)
        {
            return null;
        }

        try
        {
            string code = fiscalCode.ToUpperInvariant();

            if (!int.TryParse(code.Substring(6, 2), out int rawYear) ||
                !int.TryParse(code.Substring(9, 2), out int rawDay))
            {
                return null;
            }

            char monthChar = code[8];
            int month = MonthCodes.IndexOf(monthChar) + 1;
            if (month <= 0)
            {
                return null;
            }

            bool isFemale = rawDay > 40;
            int day = isFemale ? rawDay - 40 : rawDay;

            int currentTwoDigitYear = DateTime.Now.Year % 100;
            int year = rawYear <= currentTwoDigitYear ? 2000 + rawYear : 1900 + rawYear;

            var birthDate = new DateTime(year, month, day);

            string rawCadastralCode = code.Substring(11, 4);
            string cadastralCode = rawCadastralCode.NormalizeCadastralCode();

            if (!_cadastralDictionary.TryGetValue(cadastralCode, out string? placeOfBirth))
            {
                placeOfBirth = "UNKNOWN LOCATION";
            }

            return new FiscalCodeInfo(birthDate, isFemale, cadastralCode, placeOfBirth);
        }
        catch
        {
            return null;
        }
    }
}