using System.Globalization;

namespace ErikForwerk.Localization.WPF.Interfaces;

public interface ILocalizationCore
{
	CultureInfo CurrentCulture
		{ get; set;}

	IEnumerable<CultureInfo> SupportedCultures
		{ get; }

	void AddTranslations(ISingleCultureDictionary dictionary);
}