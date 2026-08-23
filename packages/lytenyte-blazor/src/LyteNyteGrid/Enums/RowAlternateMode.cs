namespace LyteNyteGrid.Enums;

/// <summary>
/// Controls how alternate row striping is applied.
/// Mirrors the React prop which accepts boolean | "root".
/// </summary>
public enum RowAlternateMode
{
    /// <summary>Row striping is disabled.</summary>
    None,

    /// <summary>Alternate rows are striped (standard behavior).</summary>
    Enabled,

    /// <summary>Only root-level rows are striped (groups count, nested children do not).</summary>
    Root
}
