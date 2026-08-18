namespace ActiveDirectory.Core.Interfaces;

public record FiscalCodeInfo(DateTime BirthDate, bool IsFemale, string IstatCode, string PlaceOfBirth);

public interface IFiscalCodeDecoder
{
    FiscalCodeInfo? Decode(string fiscalCode);
}