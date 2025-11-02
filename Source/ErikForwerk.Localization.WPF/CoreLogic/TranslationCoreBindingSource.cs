
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
internal sealed partial class TranslationCoreBindingSource : INotifyPropertyChanged, ILocalizationCore, IDisposable
{
	//-----------------------------------------------------------------------------------------------------------------
	#region Nested Types

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

	private readonly CultureInfo _originalThreadCulture = Thread.CurrentThread.CurrentUICulture;
	private CultureInfo _currentCulture = Thread.CurrentThread.CurrentUICulture;
	private readonly Dictionary<CultureInfo, ISingleCultureDictionary> _dictionaries = [];
	private readonly List<PropertyChangedEventHandler?> _propertyChangedHandlers = [];

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
			existingDictionary.AddOrUpdate(dictionary);

		else
		{
			_dictionaries[dictionary.Culture] = dictionary;
			RaisePropertyChanged(nameof(SupportedCultures));	//--- a new culture has been added: update supported cultures ---
		}

		//--- translations have been added or changed: update all bindings ---
		if (dictionary.Culture.Equals(_currentCulture))
			RaisePropertyChanged(nameof(LocalizedText));
	}

	#endregion ILocalizationCore

	//-----------------------------------------------------------------------------------------------------------------
	#region Private Methods

	[GeneratedRegex(@"%([^%]+)%")]
	private static partial Regex MatchPlaceholderRegEx();

	#endregion Private Methods

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
		foreach (PropertyChangedEventHandler? handler in _propertyChangedHandlers)
			_propertyChanged -= handler;

		_propertyChangedHandlers.Clear();

		//--- reset dictionaries and cultures ---
		_dictionaries.Clear();
		RaisePropertyChanged(nameof(SupportedCultures));

		_currentCulture							= _originalThreadCulture;
		RaisePropertyChanged(nameof(CurrentCulture));
		RaisePropertyChanged(nameof(LocalizedText));

		Thread.CurrentThread.CurrentUICulture	= _originalThreadCulture;
	}

	#endregion Public Methods

	//-----------------------------------------------------------------------------------------------------------------
	#region INotifyPropertyChanged

	[SuppressMessage("Style", "IDE1006:Benennungsstile", Justification = "Behaves like a field here")]
	private event PropertyChangedEventHandler? _propertyChanged;

	//--- track handler so they can be removed when Disposing/Resetting ---
	public event PropertyChangedEventHandler? PropertyChanged
		{
		add
		{
			_propertyChanged += value;
			_propertyChangedHandlers.Add(value);
		}
		remove
		{
			_propertyChanged -= value;
			_ = _propertyChangedHandlers.Remove(value);
		}
	}

	public void RaisePropertyChanged([CallerMemberName] string propertyName = "")
		=> _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	#endregion INotifyPropertyChanged
}