namespace ErikForwerk.Localization.WPF.Models;

/// <summary>
/// Parameter object for passing placeholder parsing configuration to converters.
/// </summary>
internal readonly record struct PlaceholderConverterParameter(string? DesignTimeFallback, bool ParsePlaceholders);
