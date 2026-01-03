
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

using ErikForwerk.Localization.WPF.Enums;
using ErikForwerk.Localization.WPF.Interfaces;
using ErikForwerk.Localization.WPF.Tools;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.CoreLogic;

//-----------------------------------------------------------------------------------------------------------------------------------------
internal sealed partial class TranslationCoreBindingSource : ITranslationChanged, ILocalizationCore, IDisposable
{
	//-----------------------------------------------------------------------------------------------------------------
	#region Nested Types

	/// <summary>
	/// Provides a mechanism to temporarily replace the current <see cref="TranslationCoreBindingSource"/>
	/// instance for testing purposes. Restores the original instance when disposed.
	/// </summary>
	/// <remarks>
	/// Use this class to isolate changes to the <see cref="TranslationCoreBindingSource"/> during test execution.
	/// Upon disposal, the original instance is restored to ensure test isolation and prevent side effects.
	/// </remarks>
	internal sealed class TestModeTracker : IDisposable
	{
		//-----------------------------------------------------------------------------------------------------------------
		#region Fields

		private readonly TranslationCoreBindingSource _originalInstance;

		#endregion Fields

		//-----------------------------------------------------------------------------------------------------------------
		#region Construction
		public TestModeTracker()
		{
			_originalInstance	= Instance;
			Instance			= new TranslationCoreBindingSource();
		}
		public void Dispose()
		{
			Instance.Dispose();
			Instance = _originalInstance;

			GC.SuppressFinalize(this);
		}

		#endregion Construction
	}

	#endregion Nested Types

	//-----------------------------------------------------------------------------------------------------------------
	#region Fields

	private readonly CultureInfo _originalThreadUICulture	= Thread.CurrentThread.CurrentUICulture;
	private CultureInfo _currentCulture						= Thread.CurrentThread.CurrentUICulture;
	private readonly Dictionary<CultureInfo, ISingleCultureDictionary> _dictionaries = [];

	#endregion Fields

	//-----------------------------------------------------------------------------------------------------------------
	#region Construction

	private TranslationCoreBindingSource()
	{ }

	public void Dispose()
	{
		Reset();
		GC.SuppressFinalize(this);
	}

	public static TranslationCoreBindingSource Instance
		{ get; private set; } = new TranslationCoreBindingSource();

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
			//Thread.CurrentThread.CurrentUICulture = value;
			RaiseLocalizationChanged(ELocalizationChanges.CurrentCulture);
		}
	}

	public IEnumerable<CultureInfo> SupportedCultures
		=> _dictionaries.Keys;

	public void AddTranslations(IEnumerable<ISingleCultureDictionary> dictionaries)
	{
		ArgumentNullException.ThrowIfNull(dictionaries);
		ELocalizationChanges changes = ELocalizationChanges.None;

		//--- process all dictionaries ---
		foreach (ISingleCultureDictionary dictionary in dictionaries)
		{
			ArgumentNullException.ThrowIfNull(dictionary);

			//--- Merge dictionaries if culture already exists ---
			if (_dictionaries.TryGetValue(dictionary.Culture, out ISingleCultureDictionary? existingDictionary))
				existingDictionary.AddOrUpdate(dictionary);
			//--- add completely new dictionary ---
			else
			{
				_dictionaries[dictionary.Culture] = dictionary;
				//--- a new culture has been added: update supported cultures ---
				changes |= ELocalizationChanges.SupportedCultures;
			}
			//--- the current culture has been added/updated: update localized text ---
			if (dictionary.Culture.Equals(_currentCulture))
				changes |= ELocalizationChanges.Translations;
		}

		//--- translations have been added or changed: update all bindings ---
		RaiseLocalizationChanged(changes);
	}


	public void AddTranslations(ISingleCultureDictionary dictionary)
		=> AddTranslations([dictionary]);

	public void AddTranslations(ILocalizationReader reader)
	{
		ArgumentNullException.ThrowIfNull(reader);
		AddTranslations(reader.GetLocalizations());
	}

	#endregion ILocalizationCore

	//-----------------------------------------------------------------------------------------------------------------
	#region Private Methods

	[GeneratedRegex(@"%([^%]+)%")]
	private static partial Regex MatchPlaceholderRegEx();

	#endregion Private Methods

	//-----------------------------------------------------------------------------------------------------------------
	#region Public Methods

	public string GetTranslation(string key)
		=> GetTranslation(key, parsePlaceholders: false);

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

	private static string ParseLocalizedText(ISingleCultureDictionary existingDictionary, string text)
	{
		return MatchPlaceholderRegEx().Replace(text, match =>
		{
			string innerKey = match.Groups[1].Value;
			return existingDictionary.GetTranslation(innerKey);
		});
	}

	/// <summary>
	/// Resets the localization state by clearing all dictionaries and updating the current culture to match the thread's UI culture.
	/// Raises property change notifications for supported cultures and localized text.
	/// </summary>
	/// <remarks>Call this method to reinitialize localization data, typically after changing available cultures or
	/// updating resource dictionaries. Property change notifications allow data-bound UI elements to refresh their
	/// displayed values accordingly.</remarks>
	private void Reset()
	{
		//--- remove all property changed handlers ---
		_translationChangedHandlers.Clear();

		//--- reset dictionaries and cultures ---
		_dictionaries.Clear();

		_currentCulture							= _originalThreadUICulture;
		//Thread.CurrentThread.CurrentUICulture	= _originalThreadCulture;

		RaiseLocalizationChanged(ELocalizationChanges.All);
	}


	#endregion Public Methods

	//-----------------------------------------------------------------------------------------------------------------
	#region INotifyPropertyChanged

	private readonly HashSet<ITranslationChanged.LocalizationChangedHandler> _translationChangedHandlers = [];

	public void RegisterCallback(ITranslationChanged.LocalizationChangedHandler callback)
		=> _translationChangedHandlers.Add(callback);

	public void UnregisterCallback(ITranslationChanged.LocalizationChangedHandler callback)
		=> _translationChangedHandlers.Remove(callback);

	private void RaiseLocalizationChanged(ELocalizationChanges changes)
	{
		foreach (ITranslationChanged.LocalizationChangedHandler handler in _translationChangedHandlers)
			handler(changes);
	}

	#endregion INotifyPropertyChanged
}