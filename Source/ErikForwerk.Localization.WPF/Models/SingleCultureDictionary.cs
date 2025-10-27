
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Transactions;

using ErikForwerk.Localization.WPF.Interfaces;
using ErikForwerk.Localization.WPF.Tools;

namespace ErikForwerk.Localization.WPF.Models;

public sealed class SingleCultureDictionary(CultureInfo culture) : ISingleCultureDictionary
{
	private readonly Dictionary<string, string> _translations
		= new (StringComparer.OrdinalIgnoreCase);

	public CultureInfo Culture
		{ get; } = culture ?? throw new ArgumentNullException(nameof(culture));

	public void AddOrUpdate(string key, string translation)
	{
		ArgumentNullException.ThrowIfNull(key);
		_translations[string.Intern(key)] = translation ?? string.Empty;
	}

	public void AddOrUpdate(ISingleCultureDictionary otherDict)
	{
		ArgumentNullException.ThrowIfNull(otherDict);

		if (otherDict.Culture.Name != Culture.Name)
			throw new ArgumentException("The provided dictionary has a different culture.", nameof(otherDict));

		foreach (KeyValuePair<string, string> kvp in otherDict.GetAllTranslations())
	 		_translations[kvp.Key] = kvp.Value;
	}

	//public bool TryGetTranslation(string key, out string translation)
	//{
	//	ArgumentNullException.ThrowIfNull(key);
	//
	//	bool result = _translations.TryGetValue(key, out translation!);
	//	translation ??= key.FormatAsNotTranslated();
	//	return result;
	//}
	public bool ContainsKey(string key)
	{
		ArgumentNullException.ThrowIfNull(key);
		return _translations.ContainsKey(key);
	}

	public string GetTranslation(string key)
	{
		ArgumentNullException.ThrowIfNull(key);

		//_ = TryGetTranslation(key, out string translation);
		//return translation;

		ArgumentNullException.ThrowIfNull(key);

		return _translations.TryGetValue(key, out string? translation)
			? translation
			: key.FormatAsNotTranslated();
	}

	public IReadOnlyDictionary<string, string> GetAllTranslations()
		=> _translations;
}
