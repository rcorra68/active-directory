using System.Globalization;
using ActiveDirectory.Core.Interfaces;

namespace ActiveDirectory.Infrastructure.Services;

public class FiscalCodeDecoder : IFiscalCodeDecoder
{
    private const string MonthCodes = "ABCDEHLMPRST";
    private readonly Dictionary<string, string> _catastoDictionary;

    public FiscalCodeDecoder(Dictionary<string, string> catastoDictionary)
    {
        _catastoDictionary = catastoDictionary ?? new Dictionary<string, string>();
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
            if (month <= 0) return null;

            bool isFemale = rawDay > 40;
            int day = isFemale ? rawDay - 40 : rawDay;

            int currentTwoDigitYear = DateTime.Now.Year % 100;
            int year = rawYear <= currentTwoDigitYear ? 2000 + rawYear : 1900 + rawYear;

            var birthDate = new DateTime(year, month, day);
            string istatCode = code.Substring(11, 4);

            _catastoDictionary.TryGetValue(istatCode, out string? placeOfBirth);
            placeOfBirth ??= "COMUNE SCONOSCIUTO";

            return new FiscalCodeInfo(birthDate, isFemale, istatCode, placeOfBirth);
        }
        catch
        {
            return null;
        }
    }
}