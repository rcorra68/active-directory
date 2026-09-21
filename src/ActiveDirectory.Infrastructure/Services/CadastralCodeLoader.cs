using ActiveDirectory.Core.Interfaces;
using ActiveDirectory.Core.Models;
using ActiveDirectory.Infrastructure.Helpers;
using ActiveDirectory.Infrastructure.Mappings;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Reflection;

namespace ActiveDirectory.Infrastructure.Services;

public class CadastralCodeLoader : ICadastralCodeLoader
{
    private const string ItalianMunicipalitiesResource = "ActiveDirectory.Infrastructure.Data.codici_comuni.csv";
    private const string ForeignStatesResource = "ActiveDirectory.Infrastructure.Data.stati_esteri.csv";

    public IReadOnlyDictionary<string, string> LoadCadastralCodes()
    {
        var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        LoadCsvIntoDictionary<CadastralCodeRecordMap>(ItalianMunicipalitiesResource, dictionary);

        LoadCsvIntoDictionary<ForeignStateRecordMap>(
            ForeignStatesResource,
            dictionary,
            record => !string.IsNullOrWhiteSpace(record.Code) &&
                      !record.Code.Trim().Equals("n.d.", StringComparison.OrdinalIgnoreCase));

        return dictionary;
    }

    private static void LoadCsvIntoDictionary<TMap>(
        string resourceName,
        Dictionary<string, string> dictionary,
        Func<CadastralCodeRecord, bool>? filter = null)
        where TMap : ClassMap<CadastralCodeRecord>
    {
        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new FileNotFoundException($"Embedded resource '{resourceName}' was not found in assembly.");
        }

        using var reader = new StreamReader(stream);
        var firstLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(firstLine))
        {
            return;
        }

        stream.Position = 0;
        reader.DiscardBufferedData();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = firstLine.Contains(';') ? ";" : ",",
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            PrepareHeaderForMatch = args => args.Header.Trim().ToUpperInvariant(),
            TrimOptions = TrimOptions.Trim
        };

        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<TMap>();

        foreach (var record in csv.GetRecords<CadastralCodeRecord>())
        {
            if (filter != null && !filter(record))
            {
                continue;
            }

            var cleanCode = record.Code.NormalizeCadastralCode();
            var cleanName = record.Name?.Trim();
            var cleanProvince = record.Province?.Trim();

            if (string.IsNullOrEmpty(cleanCode) || string.IsNullOrEmpty(cleanName))
            {
                continue;
            }

            // Comuni italiani: "AGUGLIANO (AN)". Stati esteri: provincia assente, resta solo il nome.
            var placeName = string.IsNullOrEmpty(cleanProvince)
                ? cleanName
                : $"{cleanName} ({cleanProvince})";

            dictionary[cleanCode] = placeName;
        }
    }
}