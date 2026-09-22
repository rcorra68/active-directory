using ActiveDirectory.Infrastructure.Services;
using Xunit;

namespace ActiveDirectory.Infrastructure.Tests.Services;

public class FiscalCodeDecoderTests
{
    // Usa il loader reale: verifica end-to-end che il dizionario costruito da
    // CadastralCodeLoader venga correttamente sfruttato da FiscalCodeDecoder.
    private readonly FiscalCodeDecoder _sut;

    public FiscalCodeDecoderTests()
    {
        var cadastralDictionary = new CadastralCodeLoader().LoadCadastralCodes();
        _sut = new FiscalCodeDecoder(cadastralDictionary);
    }

    [Fact]
    public void Decode_ShouldResolveForeignBirthPlace_Argentina()
    {
        var result = _sut.Decode("CRFCLS84A07Z600L");

        Assert.NotNull(result);
        Assert.Equal("ARGENTINA", result!.PlaceOfBirth);
    }

    [Fact]
    public void Decode_ShouldResolveForeignBirthPlace_UnitedKingdom()
    {
        // CF sintetico: formato valido, carattere di controllo non verificato dal decoder.
        var result = _sut.Decode("XXXYYY84A07Z114X");

        Assert.NotNull(result);
        Assert.Equal("REGNO UNITO", result!.PlaceOfBirth);
    }

    [Fact]
    public void Decode_ShouldAppendProvince_ForItalianMunicipality()
    {
        // CF sintetico costruito sul codice catastale A001 (Abano Terme, PD).
        var result = _sut.Decode("XXXYYY84A07A001X");

        Assert.NotNull(result);
        Assert.Equal("ABANO TERME (PD)", result!.PlaceOfBirth);
    }

    [Fact]
    public void Decode_ShouldReturnUnknownLocation_ForUnrecognizedCadastralCode()
    {
        var result = _sut.Decode("XXXYYY84A07Z999X");

        Assert.NotNull(result);
        Assert.Equal("UNKNOWN LOCATION", result!.PlaceOfBirth);
    }
}