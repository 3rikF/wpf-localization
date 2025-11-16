using System.Globalization;
using System.Windows.Data;
using System.ComponentModel;
using ErikForwerk.Localization.WPF.Tools;
using System.Windows;
using System.Diagnostics.CodeAnalysis;
using ErikForwerk.Localization.WPF.Models;

namespace ErikForwerk.Localization.WPF.CoreLogic;

/// <summary>
/// Converter for dynamic localization keys from bindings
/// </summary>
internal sealed class LocalizationDynamicTextConverter : IMultiValueConverter
{
	public LocalizationDynamicTextConverter()
	{ }

	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		PlaceholderConverterParameter? converterParam = parameter as PlaceholderConverterParameter?;

		if (values.Length > 0 && values[0] == DependencyProperty.UnsetValue)
			return (converterParam?.DesignTimeFallback ?? "UnknwownBindingPath").FormatAsNotTranslated();

		else if (values.Length < 1 || values[0] is null)
			return string.Empty;

		else
		{
			object value = values[0]!;

			// If the bound value is already a string, use it directly as the key.
			// For any other type, format the key as "TypeName.Value" (e.g. "Status.Active").
			string key = value is string str
				? str
				: string.Format(CultureInfo.InvariantCulture, "{0}.{1}", value.GetType().Name, value);

			bool parsePlaceholders	= converterParam?.ParsePlaceholders ?? false;
			return TranslationCoreBindingSource.Instance.GetTranslation(key, parsePlaceholders);
		}
	}

	[DoesNotReturn]
	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		=> throw new NotSupportedException();
}