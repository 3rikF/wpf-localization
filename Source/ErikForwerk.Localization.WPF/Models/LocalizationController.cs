
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

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
	#region Fields

	private readonly ILocalizationCore _localizationCore;

	#endregion Fields

	//-----------------------------------------------------------------------------------------------------------------
	#region Construction

	private LocalizationController(ILocalizationCore localizationCore)
	{
		_localizationCore = localizationCore;
	}

	public LocalizationController()
		: this(TranslationCoreBindingSource.Instance)
	{ }

	internal static LocalizationController CreateUnitTestInstance(ILocalizationCore unitTestCore)
		=> new(unitTestCore);

	#endregion Construction

	//-----------------------------------------------------------------------------------------------------------------
	#region Properties

	public CultureInfo CurrentCulture
	{
		get => _localizationCore.CurrentCulture;
		set => _localizationCore.CurrentCulture = value;
	}

	public IEnumerable<CultureInfo> SupportedCultures
		=> _localizationCore.SupportedCultures;

	#endregion Properties

	//-----------------------------------------------------------------------------------------------------------------
	#region Public Methods

	public void AddTranslations(ISingleCultureDictionary dictionary)
		=> _localizationCore.AddTranslations(dictionary);

	public void AddTranslations(IEnumerable<ISingleCultureDictionary> dictionaries)
		=> _localizationCore.AddTranslations(dictionaries);

	public void AddTranslations(ILocalizationReader reader)
		=> _localizationCore.AddTranslations(reader);

	public void AddTranslationsFromCsvResource(string resourcePath)
		=> AddTranslations(new RessourceCsvReader(resourcePath, Assembly.GetCallingAssembly()));

	#endregion Public Methods
}
