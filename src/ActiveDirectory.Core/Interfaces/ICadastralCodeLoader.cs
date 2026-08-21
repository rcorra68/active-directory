using System.Collections.Generic;

namespace ActiveDirectory.Core.Interfaces;

/// <summary>
/// Service contract for loading cadastral codes and municipality mappings.
/// </summary>
public interface ICadastralCodeLoader
{
    /// <summary>
    /// Loads cadastral code mapping records from the embedded CSV dataset.
    /// </summary>
    /// <returns>A read-only dictionary with cadastral code keys and place names as values.</returns>
    IReadOnlyDictionary<string, string> LoadCadastralCodes();
}