using CsvHelper.Configuration;
using ActiveDirectory.Core.Models;

namespace ActiveDirectory.Infrastructure.Mappings;

/// <summary>
/// Maps the ISTAT/MAE "stati esteri" CSV (foreign states cadastral codes) onto the shared
/// CadastralCodeRecord model. The fiscal-code cadastral code is in "Codice AT" (e.g. Z600
/// for Argentina); Province is intentionally left unmapped since it doesn't apply here.
/// </summary>
public sealed class ForeignStateRecordMap : ClassMap<CadastralCodeRecord>
{
    public ForeignStateRecordMap()
    {
        Map(m => m.Code)
            .Name("CODICE AT");
        Map(m => m.Name)
            .Name("DENOMINAZIONE IT");
    }
}