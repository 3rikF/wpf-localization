using ErikForwerk.Localization.WPF.Models;

namespace ErikForwerk.Localization.WPF.Interfaces;

public interface ILocalizationReader
{
	/// <summary>
	/// Retrieves all available localizations as an array of single-culture dictionaries.
	/// </summary>
	/// <remarks>
	/// Currently a single file is planned to contains only a set of translations for a single language but
	/// returning an array allows for future extensions where multiple cultures could be supported in a single read operation.
	/// </remarks>
	/// <returns>
	/// An array of <see cref="SingleCultureDictionary"/> objects, each representing the localized resources for a specific culture.
	/// The array will be empty if no localizations are available.
	/// </returns>
	ISingleCultureDictionary[] GetLocalizations();
}
