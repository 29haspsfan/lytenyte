using LyteNyteGrid.Enums;

namespace LyteNyteGrid.Models;

/// <summary>
/// Base class for filter definitions.
/// </summary>
public abstract class FilterDefinition
{
    /// <summary>The column ID this filter applies to.</summary>
    public required string ColumnId { get; init; }
}

/// <summary>
/// A filter for string-based column data.
/// </summary>
public class StringFilter : FilterDefinition
{
    public FilterStringOperator Operator { get; set; } = FilterStringOperator.Contains;
    public string? Value { get; set; }
    public bool CaseInsensitive { get; set; } = true;
    public bool TrimWhitespace { get; set; }
}

/// <summary>
/// A filter for numeric column data.
/// </summary>
public class NumberFilter : FilterDefinition
{
    public FilterNumberOperator Operator { get; set; } = FilterNumberOperator.Equals;
    public double? Value { get; set; }
    public double? Epsilon { get; set; }
    public bool AbsoluteValue { get; set; }
}

/// <summary>
/// A filter for date-based column data.
/// </summary>
public class DateFilter : FilterDefinition
{
    public FilterDateOperator Operator { get; set; } = FilterDateOperator.Equals;
    public DateTimeOffset? DateValue { get; set; }
    public int? NumericValue { get; set; }
    public bool IncludeTime { get; set; }
}

/// <summary>
/// A custom filter using a predicate function.
/// </summary>
public class CustomFilter<T> : FilterDefinition
{
    public required Func<T, bool> Predicate { get; init; }
}
