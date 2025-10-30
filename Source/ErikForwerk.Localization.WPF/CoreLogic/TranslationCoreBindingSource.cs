
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using ErikForwerk.Localization.WPF.Interfaces;
using ErikForwerk.Localization.WPF.Tools;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.CoreLogic;

//-----------------------------------------------------------------------------------------------------------------------------------------
internal sealed partial class TranslationCoreBindingSource : INotifyPropertyChanged, ILocalizationCore
{
	//-----------------------------------------------------------------------------------------------------------------
	#region Fields

	private CultureInfo _currentCulture = Thread.CurrentThread.CurrentUICulture;
	private readonly Dictionary<CultureInfo, ISingleCultureDictionary> _dictionaries = [];

	#endregion Fields

	//-----------------------------------------------------------------------------------------------------------------
	#region Construction

	private TranslationCoreBindingSource()
	{ }

	public static TranslationCoreBindingSource Instance
		{ get; } = new TranslationCoreBindingSource();

	#endregion Construction

	//-----------------------------------------------------------------------------------------------------------------
	#region ILocalizationCore

	/// <summary>
	///Gets the localized text value for data binding scenarios.
	/// </summary>
	/// <remarks>
	/// This property is intended to be used as a binding target in UI frameworks.
	/// The returned value is always an empty string; the actual localized content is
	/// provided via the binding context by virtue of a converter using the associated key.
	/// </remarks>
	[SuppressMessage("Performance", "CA1822:Mark member as static", Justification = "Cannot be static. Needed for property-binding.")]
	public string LocalizedText
		=> string.Empty;

	public CultureInfo CurrentCulture
	{
		get => _currentCulture;
		set
		{
			if (value.Equals(_currentCulture))
				return;

			_currentCulture = value;
			Thread.CurrentThread.CurrentUICulture = value;
			RaisePropertyChanged();
			RaisePropertyChanged(nameof(LocalizedText));
		}
	}

	public IEnumerable<CultureInfo> SupportedCultures
		=> _dictionaries.Keys;

	public void AddTranslations(ISingleCultureDictionary dictionary)
	{
		ArgumentNullException.ThrowIfNull(dictionary);

		//--- Merge dictionaries if culture already exists ---
		if (_dictionaries.TryGetValue(dictionary.Culture, out ISingleCultureDictionary? existingDictionary))
		{
			existingDictionary.AddOrUpdate(dictionary);

			if (dictionary.Culture.Equals(_currentCulture))
				RaisePropertyChanged(nameof(LocalizedText));
		}

		else
		{
			_dictionaries[dictionary.Culture] = dictionary;
			RaisePropertyChanged(nameof(SupportedCultures));
			RaisePropertyChanged(nameof(LocalizedText));
		}
	}

	#endregion ILocalizationCore

	//-----------------------------------------------------------------------------------------------------------------
	#region Public Methods

	public string GetTranslation(string key, bool parsePlaceholders)
	{
		if (string.IsNullOrEmpty(key))
			return string.Empty;

		if (_dictionaries.TryGetValue(_currentCulture, out ISingleCultureDictionary? existingDictionary))
		{
			if (parsePlaceholders)
				return ParseLocalizedText(existingDictionary, key/*, []*/);
			else
				return existingDictionary.GetTranslation(key);
		}
		else
			return key.FormatAsNotTranslated();
	}

	private string ParseLocalizedText(ISingleCultureDictionary existingDictionary, string text/*, HashSet<string> visitedKeys*/)
	{
		return MatchPlaceholderRegEx().Replace(text, match =>
		{
			string innerKey = match.Groups[1].Value;
			//if (visitedKeys.Contains(innerKey))
			//	return $"%{innerKey}%"; // Return as is to avoid cycle

			//_ = visitedKeys.Add(innerKey);
			return existingDictionary.GetTranslation(innerKey);
			//string result = GetTranslationInternal(innerKey, visitedKeys);
			//_ = visitedKeys.Remove(innerKey);
			//return result;
		});
	}

	//private string GetTranslationInternal(string key, HashSet<string> visitedKeys)
	//{
	//	if (string.IsNullOrEmpty(key))
	//		return string.Empty;
	//
	//	else if (_dictionaries.TryGetValue(_currentCulture, out ISingleCultureDictionary? existingDictionary))
	//	{
	//		string translated = existingDictionary.GetTranslation(key);
	//		return ParseLocalizedText(translated, visitedKeys);
	//	}
	//
	//	else
	//		return key.FormatAsNotTranslated();
	//}

	internal void Reset()
	{
		_dictionaries.Clear();
		_currentCulture = Thread.CurrentThread.CurrentUICulture;

		RaisePropertyChanged(nameof(SupportedCultures));
		RaisePropertyChanged(nameof(LocalizedText));
	}

	#endregion Public Methods

	//-----------------------------------------------------------------------------------------------------------------
	#region INotifyPropertyChanged

	public event PropertyChangedEventHandler? PropertyChanged;

	public void RaisePropertyChanged([CallerMemberName] string propertyName = "")
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	[GeneratedRegex(@"%([^%]+)%")]
	private static partial Regex MatchPlaceholderRegEx();

	#endregion INotifyPropertyChanged
}