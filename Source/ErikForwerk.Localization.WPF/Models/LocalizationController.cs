
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Markup;

using ErikForwerk.Localization.WPF.CoreLogic;
using ErikForwerk.Localization.WPF.Interfaces;
using ErikForwerk.Localization.WPF.Reader;

//--- for mocking type-internal types (i.e. IWindow) ---
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Models;

//-----------------------------------------------------------------------------------------------------------------------------------------
/// <summary>
/// Provides access to localization functionality, including managing the
/// current culture and retrieving supported cultures for the application.
/// </summary>
/// <remarks>
/// This controller serves as the main entry point for localization operations.
/// It delegates localization logic to an underlying implementation of the ILocalizationCore interface.
/// The controller is sealed and cannot be inherited.
/// </remarks>
public sealed class LocalizationController : ILocalizationCore
{
	//-----------------------------------------------------------------------------------------------------------------
	#region Nested Types

	internal interface IWindow
	{
		XmlLanguage Language
			{ get; set; }
	}

	// TODO: Use TestBase-class and from there, [RunInStaThread] to enable unit testing of this class.
	[ExcludeFromCodeCoverage(Justification = "Can only be tested in an STA")]
	private readonly record struct WindowWrapper(Window Window) : IWindow
	{
		public XmlLanguage Language
		{
			get => Window.Language;
			set => Window.Language = value;
		}
	}

	#endregion Nested Types

	//-----------------------------------------------------------------------------------------------------------------
	#region Fields

	private readonly IWindow _window;
	private readonly ILocalizationCore _localizationCore;

	#endregion Fields

	//-----------------------------------------------------------------------------------------------------------------
	#region Construction

	private LocalizationController(IWindow targetWindow, ILocalizationCore localizationCore)
	{
		_localizationCore	= localizationCore;
		_window				= targetWindow;

		UpdateWindowLanguage();
	}

	// TODO: Use TestBase-class and from there, [RunInStaThread] to enable unit testing of this constructor.
	[ExcludeFromCodeCoverage(Justification = "Can only be tested in an STA")]
	public LocalizationController(Window targetWindow)
		: this(new WindowWrapper(targetWindow), TranslationCoreBindingSource.Instance)
	{ }

	internal static LocalizationController CreateUnitTestInstance(IWindow unitTestWindow, ILocalizationCore unitTestCore)
		=> new(unitTestWindow, unitTestCore);

	#endregion Construction

	//-----------------------------------------------------------------------------------------------------------------
	#region Properties

	public CultureInfo CurrentCulture
	{
		get => _localizationCore.CurrentCulture;
		set
		{
			if (value == _localizationCore.CurrentCulture)
				return;

			_localizationCore.CurrentCulture = value;
			UpdateWindowLanguage();
		}
	}

	public IEnumerable<CultureInfo> SupportedCultures
		=> _localizationCore.SupportedCultures;

	#endregion Properties

	//-----------------------------------------------------------------------------------------------------------------
	#region Private Methods

	private void UpdateWindowLanguage()
	{
		if (_window is not null)
			_window.Language = XmlLanguage.GetLanguage(CurrentCulture.IetfLanguageTag);
	}

	#endregion Private Methods

	//-----------------------------------------------------------------------------------------------------------------
	#region Public Methods

	public void AddTranslations(ISingleCultureDictionary dictionary)
		=> _localizationCore.AddTranslations(dictionary);

	public void AddTranslationsFromCsvResource(string resourcePath)
	{
		RessourceCsvReader reader = new(resourcePath, Assembly.GetCallingAssembly());

		foreach (ISingleCultureDictionary dict in reader.GetLocalizations())
			_localizationCore.AddTranslations(dict);
	}

	#endregion Public Methods
}
