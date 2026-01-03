
using System.Globalization;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Interfaces;

//-----------------------------------------------------------------------------------------------------------------------------------------
public interface ILocalizationCore
{
	CultureInfo CurrentCulture
		{ get; set;}

	IEnumerable<CultureInfo> SupportedCultures
		{ get; }

	void AddTranslations(ISingleCultureDictionary dictionary);

	void AddTranslations(IEnumerable<ISingleCultureDictionary> dictionaries);

	void AddTranslations(ILocalizationReader reader);

	string GetTranslation(string key);

	string GetTranslation(string key, bool parsePlaceholders);
}