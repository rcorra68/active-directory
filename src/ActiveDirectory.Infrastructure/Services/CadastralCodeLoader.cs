using ActiveDirectory.Core.Interfaces;
using ActiveDirectory.Core.Models;
using ActiveDirectory.Infrastructure.Helpers;
using ActiveDirectory.Infrastructure.Mappings;
using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace ActiveDirectory.Infrastructure.Services;

public class CadastralCodeLoader : ICadastralCodeLoader
{
    private const string ResourceNamespace = "ActiveDirectory.Infrastructure.Data.codici_comuni.csv";

    public IReadOnlyDictionary<string, string> LoadCadastralCodes()
    {
        var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(ResourceNamespace);
        if (stream == null)
        {
            throw new FileNotFoundException($"Embedded resource '{ResourceNamespace}' was not found in assembly.");
        }

        using var reader = new StreamReader(stream);
        var firstLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(firstLine))
        {
            return dictionary;
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
        csv.Context.RegisterClassMap<CadastralCodeRecordMap>();

        foreach (var record in csv.GetRecords<CadastralCodeRecord>())
        {
            var cleanCode = record.Code.NormalizeCadastralCode();
            var cleanName = record.Name?.Trim();

            if (!string.IsNullOrEmpty(cleanCode) && !string.IsNullOrEmpty(cleanName))
            {
                dictionary[cleanCode] = cleanName;
            }
        }

        return dictionary;
    }
}