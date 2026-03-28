
using System.Collections;
using System.Globalization;

using ErikForwerk.Localization.WPF.Interfaces;
using ErikForwerk.Localization.WPF.Tools;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Models;

//-----------------------------------------------------------------------------------------------------------------------------------------
public sealed class SingleCultureDictionary(CultureInfo culture) : ISingleCultureDictionary
{
	private readonly Dictionary<string, string> _translations
		= new (StringComparer.OrdinalIgnoreCase);

	public CultureInfo Culture
		{ get; } = culture ?? throw new ArgumentNullException(nameof(culture));

	public void Add(string key, string translation)
	{
		ArgumentNullException.ThrowIfNull(key);

		if (_translations.ContainsKey(key))
			throw new ArgumentException($"An element with the key '{key}' already exists.", nameof(key));

		_translations[string.Intern(key)] = translation ?? string.Empty;
	}

	public void AddOrUpdate(string key, string translation)
	{
		ArgumentNullException.ThrowIfNull(key);
		_translations[string.Intern(key)] = translation ?? string.Empty;
	}

	public void AddOrUpdate(ISingleCultureDictionary otherDict)
	{
		ArgumentNullException.ThrowIfNull(otherDict);

		if (otherDict.Culture.Name != Culture.Name)
			throw new ArgumentException("The provided dictionary contains a different culture.", nameof(otherDict));

		foreach (KeyValuePair<string, string> kvp in otherDict.GetAllTranslations())
	 		_translations[kvp.Key] = kvp.Value;
	}

	public bool ContainsKey(string key)
	{
		ArgumentNullException.ThrowIfNull(key);

		return _translations.ContainsKey(key);
	}

	public string GetTranslation(string key)
	{
		ArgumentNullException.ThrowIfNull(key);

		return _translations.TryGetValue(key, out string? translation)
			? translation
			: key.FormatAsNotTranslated();
	}

	public IReadOnlyDictionary<string, string> GetAllTranslations()
		=> _translations;

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
		=> _translations.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();
}
