using ActiveDirectory.Infrastructure.Services;
using Xunit;

namespace ActiveDirectory.Infrastructure.Tests.Services;

public class CadastralCodeLoaderTests
{
    private readonly CadastralCodeLoader _sut = new();

    [Fact]
    public void LoadCadastralCodes_ShouldAppendProvince_ForItalianMunicipality()
    {
        var result = _sut.LoadCadastralCodes();

        Assert.True(result.TryGetValue("A001", out var placeName));
        Assert.Equal("ABANO TERME (PD)", placeName);
    }

    [Theory]
    [InlineData("Z600", "ARGENTINA")]
    [InlineData("Z114", "REGNO UNITO")]
    public void LoadCadastralCodes_ShouldResolveForeignState_WithoutProvince(string cadastralCode, string expectedName)
    {
        var result = _sut.LoadCadastralCodes();

        Assert.True(result.TryGetValue(cadastralCode, out var placeName));
        Assert.Equal(expectedName, placeName);
    }

    [Fact]
    public void LoadCadastralCodes_ShouldNotContain_ItalyPlaceholderRow()
    {
        // La riga dell'Italia nel file stati_esteri.csv ha "n.d." come Codice AT
        // e va filtrata: non deve produrre una chiave "N.D." (o simile) nel dizionario.
        var result = _sut.LoadCadastralCodes();

        Assert.DoesNotContain(result.Keys, key => key.Equals("N.D.", StringComparison.OrdinalIgnoreCase));
    }
}