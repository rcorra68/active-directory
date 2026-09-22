using CsvHelper.Configuration;
using ActiveDirectory.Core.Models;

namespace ActiveDirectory.Infrastructure.Mappings;

public sealed class CadastralCodeRecordMap : ClassMap<CadastralCodeRecord>
{
    public CadastralCodeRecordMap()
    {
        Map(m => m.Code)
            .Name("CODICE NAZIONALE");
        Map(m => m.Name)
            .Name("DENOMINAZIONE ITALIANA");
        Map(m => m.Province)
            .Name("SIGLA PROVINCIA")
            .Optional(); // vuoto per i record relativi a stati esteri
    }
}