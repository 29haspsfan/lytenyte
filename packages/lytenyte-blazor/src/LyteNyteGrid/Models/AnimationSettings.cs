namespace LyteNyteGrid.Models;

/// <summary>
/// Settings for row or column transition animations.
/// </summary>
public class AnimationSettings
{
    /// <summary>Duration of the animation in milliseconds.</summary>
    public int DurationMs { get; set; } = 300;

    /// <summary>CSS easing function name (e.g. "ease", "ease-in-out", "linear").</summary>
    public string Easing { get; set; } = "ease";

    /// <summary>Whether the animation is enabled.</summary>
    public bool Enabled { get; set; } = true;
}
