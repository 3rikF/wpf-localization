using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows.Data;

namespace ErikForwerk.Localization.WPF.CoreLogic;

/// <summary>
/// Converter for static localization keys
/// </summary>
internal sealed class LocalizationTextConverter(string _localizationKey, bool _parsePlaceholders)
	: IValueConverter
{
	/// <summary>
	/// Retrieves the localized translation for the key associated with this converter.
	/// </summary>
	/// <remarks>
	/// This converter does not use the input value, target type, or parameter.
	/// It returns the translation for the key provided at construction, using the specified culture.
	/// The translation source is notified of culture changes and triggers this converter automatically.
	/// </remarks>
	/// <param name="value">The value produced by the binding source. This parameter is ignored by the converter.</param>
	/// <param name="targetType">The type of the binding target property. This parameter is ignored by the converter.</param>
	/// <param name="parameter">An optional parameter to be used in the conversion logic. This parameter is ignored by the converter.</param>
	/// <param name="culture">The culture to use in the converter. The translation is retrieved for this culture.</param>
	/// <returns>An object containing the localized translation for the specified key and culture.</returns>
	public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
		=> TranslationCoreBindingSource.Instance.GetTranslation(_localizationKey, _parsePlaceholders);

	[DoesNotReturn]
	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		=> throw new NotSupportedException();
}