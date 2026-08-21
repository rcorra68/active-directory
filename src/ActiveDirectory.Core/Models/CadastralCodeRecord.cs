namespace ActiveDirectory.Core.Models;

/// <summary>
/// Represents a raw cadastral entry loaded from official ISTAT / Agenzia delle Entrate CSV sources.
/// </summary>
public class CadastralCodeRecord
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}