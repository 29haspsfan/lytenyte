namespace LyteNyteGrid.Models;

/// <summary>
/// Custom styling for various grid sections.
/// </summary>
public class GridStyle
{
    public ElementStyle? Viewport { get; set; }
    public ElementStyle? Row { get; set; }
    public ElementStyle? Header { get; set; }
    public ElementStyle? Detail { get; set; }
    public ElementStyle? HeaderGroup { get; set; }
    public ElementStyle? Cell { get; set; }
}

public class ElementStyle
{
    public string? Style { get; set; }
    public string? ClassName { get; set; }
}
