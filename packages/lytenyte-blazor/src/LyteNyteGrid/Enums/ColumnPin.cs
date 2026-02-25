namespace LyteNyteGrid.Enums;

/// <summary>
/// Represents the possible pinned positions a column can occupy.
/// In LTR mode, "Start" pins to the left and "End" to the right.
/// In RTL mode, this is reversed.
/// </summary>
public enum ColumnPin
{
    None,
    Start,
    End
}
