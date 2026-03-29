
// ignore spelling: お願いします

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Transactions;
using System.Windows;

using ErikForwerk.Localization.WPF.CoreLogic;
using ErikForwerk.Localization.WPF.Interfaces;
using ErikForwerk.Localization.WPF.Models;
using ErikForwerk.TestAbstractions.Models;

using Moq;

using Xunit.Abstractions;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Tests.Models;

//-----------------------------------------------------------------------------------------------------------------------------------------
[Collection("82A46DF4-F8CA-4E66-8606-DF49164DEFBB")]
public sealed class LocalizationControllerTests : TestBase, IDisposable
{
	//-----------------------------------------------------------------------------------------------------------------
	#region Test Cleanup

	private readonly TranslationCoreBindingSource.TestModeTracker _testModetracker = new ();

	[ExcludeFromCodeCoverage(Justification = "Untestable race condition at test run time")]
	public LocalizationControllerTests(ITestOutputHelper toh)
		: base(toh)
	{
		//--- initialize WPF Application if not already done ---
		if (Application.Current is null)
		{
			try
			{
				_ = new Application();
			}
			catch
			{
				// Ignore exceptions during WPF initialization in test environments
			}
		}
	}

	public void Dispose()
	{
		_testModetracker.Dispose();
		GC.SuppressFinalize(this);
	}

	#endregion Test Cleanup

	//-----------------------------------------------------------------------------------------------------------------
	#region Test Data

	private static readonly CultureInfo TEST_CULTURE_DE = CultureInfo.GetCultureInfo("de-DE");
	private static readonly CultureInfo TEST_CULTURE_EN = CultureInfo.GetCultureInfo("en-US");
	private static readonly CultureInfo TEST_CULTURE_FR = CultureInfo.GetCultureInfo("fr-FR");

	#endregion Test Data

	//-----------------------------------------------------------------------------------------------------------------
	#region Helper Methods

	private static LocalizationController CreateTestLocalizationController(CultureInfo initialCulture, out Mock<ILocalizationCore> out_mockCore)
	{
		out_mockCore = new();
		_ = out_mockCore.SetupProperty(x => x.CurrentCulture, initialCulture);
		_ = out_mockCore.Setup(x => x.SupportedCultures).Returns([TEST_CULTURE_EN, TEST_CULTURE_DE]);
		_ = out_mockCore.Setup(x => x.AddTranslations(It.IsAny<ISingleCultureDictionary>()));

		return LocalizationController.CreateUnitTestInstance(out_mockCore.Object);
	}

	#endregion Helper Methods

	//-----------------------------------------------------------------------------------------------------------------
	#region Construction

	[Fact]
	public void Ctor_Parameterless_InitializesSuccessfully()
	{
		//--- ARRANGE ---------------------------------------------------------
		//--- ACT -------------------------------------------------------------
		LocalizationController sut = new();

		//--- ASSERT ----------------------------------------------------------
		Assert.NotNull(sut);
		Assert.NotNull(sut.CurrentCulture);
		Assert.NotNull(sut.SupportedCultures);
	}

	[Fact]
	public void CreateUnitTestInstance_WithMockCore_InitializesSuccessfully()
	{
		//--- ARRANGE ---------------------------------------------------------
		//--- ACT -------------------------------------------------------------
		LocalizationController sut = CreateTestLocalizationController(TEST_CULTURE_EN, out _);

		//--- ASSERT ----------------------------------------------------------
		Assert.NotNull(sut);
	}

	#endregion Construction

	//-----------------------------------------------------------------------------------------------------------------
	#region CurrentCulture Property

	[Fact]
	public void CurrentCulture_ReturnsCultureFromCore()
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationController sut = CreateTestLocalizationController(
			TEST_CULTURE_DE
			, out Mock<ILocalizationCore> mockCore);

		//--- ACT -------------------------------------------------------------
		CultureInfo result = sut.CurrentCulture;

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(TEST_CULTURE_DE, result);
		//--- once in the constructor and once in the property get ---
		mockCore.VerifyGet(x => x.CurrentCulture, Times.Once());
	}

	[Fact]
	public void CurrentCulture_UpdatesCultureInCoreAndWindow()
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationController sut = CreateTestLocalizationController(
			TEST_CULTURE_EN
			, out Mock<ILocalizationCore>? mockCore);
		//--- ACT -------------------------------------------------------------
		sut.CurrentCulture = TEST_CULTURE_DE;

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(TEST_CULTURE_DE, sut.CurrentCulture);

		mockCore.VerifySet(x => x.CurrentCulture = TEST_CULTURE_DE, Times.Once());
	}

	#endregion CurrentCulture Property

	//-----------------------------------------------------------------------------------------------------------------
	#region SupportedCultures Property

	[Fact]
	public void SupportedCultures_Get_ReturnsCulturesFromCore()
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationController sut = CreateTestLocalizationController(
			TEST_CULTURE_DE
			, out Mock<ILocalizationCore>? mockCore);

		//--- ACT -------------------------------------------------------------
		IEnumerable<CultureInfo> result = sut.SupportedCultures;

		//--- ASSERT ----------------------------------------------------------
		Assert.NotNull(result);
		Assert.Contains(TEST_CULTURE_EN, result);
		Assert.Contains(TEST_CULTURE_DE, result);
		mockCore.VerifyGet(x => x.SupportedCultures, Times.Once());
	}

	#endregion SupportedCultures Property

	//-----------------------------------------------------------------------------------------------------------------
	#region AddTranslations Method

	[Fact]
	public void AddTranslations_ValidDictionary_DelegatesToCore()
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationController sut = CreateTestLocalizationController(
			TEST_CULTURE_DE
			, out Mock<ILocalizationCore>? mockCore);

		List<ISingleCultureDictionary> dictionaries = new();

		//--- ACT -------------------------------------------------------------
		sut.AddTranslations(dictionaries);

		//--- ASSERT ----------------------------------------------------------
		mockCore.Verify(x => x.AddTranslations(dictionaries), Times.Once());
	}

	[Fact] // ignore spelling: bonjour
	public void AddTranslations_MultipleDictionaries_DelegatesToCoreForEach()
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationController sut = CreateTestLocalizationController(
			TEST_CULTURE_DE
			, out Mock<ILocalizationCore>? mockCore);

		SingleCultureDictionary dict1 = new(TEST_CULTURE_DE);
		dict1.AddOrUpdate("Hello", "Hallo");

		SingleCultureDictionary dict2 = new(TEST_CULTURE_FR);
		dict2.AddOrUpdate("Hello", "Bonjour");

		//--- ACT -------------------------------------------------------------
		sut.AddTranslations(dict1);
		sut.AddTranslations(dict2);

		//--- ASSERT ----------------------------------------------------------
		mockCore.Verify(x => x.AddTranslations(It.IsAny<ISingleCultureDictionary>()), Times.Exactly(2));
		mockCore.Verify(x => x.AddTranslations(dict1), Times.Once());
		mockCore.Verify(x => x.AddTranslations(dict2), Times.Once());
	}

	[Fact]
	public void AddTranslations_ValidReader_DelegatesToCore()
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationController sut = CreateTestLocalizationController(
			TEST_CULTURE_DE
			, out Mock<ILocalizationCore>? mockCore);

		Mock<ILocalizationReader> mockReader = new();
		_ = mockReader
			.Setup(x => x.GetLocalizations())
			.Returns([]);

		//--- ACT -------------------------------------------------------------
		sut.AddTranslations(mockReader.Object);

		//--- ASSERT ----------------------------------------------------------
		mockCore.Verify(x => x.AddTranslations(mockReader.Object), Times.Once());
	}


	#endregion AddTranslations Method

	//-----------------------------------------------------------------------------------------------------------------
	#region AddTranslationsFromCsvResource Method

	[Fact]
	public void AddTranslationsFromCsvResource_ValidResource_LoadsAndAddsDictionaries()
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationController sut = CreateTestLocalizationController(
			TEST_CULTURE_DE
			, out Mock<ILocalizationCore>? mockCore);

		const string RESOURCE_PATH = @"TestResources/TestTranslations.de-DE.csv";

		//--- ACT -------------------------------------------------------------
		sut.AddTranslationsFromCsvResource(RESOURCE_PATH);

		//--- ASSERT ----------------------------------------------------------
		mockCore.Verify(x => x.AddTranslations(It.IsAny<ILocalizationReader>()), Times.AtLeastOnce());
	}

	#endregion AddTranslationsFromCsvResource Method

	//-----------------------------------------------------------------------------------------------------------------
	#region Integration Tests

	[Fact]
	public void Integration_CreateUnitTestInstance_AllPropertiesWork()
	{
		//--- ARRANGE ---------------------------------------------------------
		//--- ACT -------------------------------------------------------------
		LocalizationController sut = CreateTestLocalizationController(
			TEST_CULTURE_DE
			, out Mock<ILocalizationCore>? mockCore);

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(TEST_CULTURE_DE, sut.CurrentCulture);
		Assert.NotEmpty(sut.SupportedCultures);

		// Test setting culture
		sut.CurrentCulture = TEST_CULTURE_EN;
		Assert.Equal(TEST_CULTURE_EN, sut.CurrentCulture);

		// Test adding translations
		SingleCultureDictionary dict = new(TEST_CULTURE_FR);
		sut.AddTranslations(dict);
		mockCore.Verify(x => x.AddTranslations(dict), Times.Once());
	}

	#endregion Integration Tests

	//-----------------------------------------------------------------------------------------------------------------
	#region GetTranslation Method

	[Fact]
	public void GetTranslation_WithCurrentCulture_WithKey_DelegatesToCore()
	{
		//--- ARRANGE ---------------------------------------------------------
		const string TEST_KEY			= "TestKey";
		const string EXPECTED_VALUE		= "TranslatedValue";

		LocalizationController sut = CreateTestLocalizationController(
			TEST_CULTURE_DE
			, out Mock<ILocalizationCore> mockCore);

		_ = mockCore
			.Setup(x => x.GetTranslation(TEST_KEY))
			.Returns(EXPECTED_VALUE);

		//--- ACT -------------------------------------------------------------
		string result = sut.GetTranslation(TEST_KEY);

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(EXPECTED_VALUE, result);
		mockCore.Verify(x => x.GetTranslation(TEST_KEY), Times.Once());
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetTranslation_WithCurrentCulture_WithKeyAndParsePlaceholders_DelegatesToCore(bool parsePlaceholders)
	{
		//--- ARRANGE ---------------------------------------------------------
		const string TEST_KEY			= "TestKey";
		const string EXPECTED_VALUE		= "TranslatedValue";

		LocalizationController sut = CreateTestLocalizationController(
			TEST_CULTURE_DE
			, out Mock<ILocalizationCore> mockCore);

		_ = mockCore
			.Setup(x => x.GetTranslation(TEST_KEY, parsePlaceholders))
			.Returns(EXPECTED_VALUE);

		//--- ACT -------------------------------------------------------------
		string result = sut.GetTranslation(TEST_KEY, parsePlaceholders);

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(EXPECTED_VALUE, result);
		mockCore.Verify(x => x.GetTranslation(TEST_KEY, parsePlaceholders), Times.Once());
	}

	[Theory]
	[InlineData("de-DE")]	// setup as TEST_CULTURE_DE
	[InlineData("en-US")]	// setup as TEST_CULTURE_EN
	public void GetTranslation_SpecificCulture_NoPlaceholder_RoutesToDictionary(string testCultureName)
	{
		//--- ARRANGE ---------------------------------------------------
		TestConsole.WriteLine($"Testing culture {B(testCultureName)}");

		const string TEST_KEY		= "TestKey";
		const string EXPECTED_VALUE	= "TranslatedValue";
		CultureInfo TestCulture		= CultureInfo.CreateSpecificCulture(testCultureName);

		//--- setup mock core to return expected value for the specific culture and key ---
		LocalizationController sut	= CreateTestLocalizationController(TEST_CULTURE_FR, out Mock<ILocalizationCore> mockCore);
		_ = mockCore
			.Setup(x => x.GetTranslation(TestCulture, TEST_KEY))
			.Returns(EXPECTED_VALUE);

		//--- ACT -------------------------------------------------------
		string translation = sut.GetTranslation(TestCulture, TEST_KEY);

		//--- ASSERT -----------------------------------------------------
		Assert.Equal(EXPECTED_VALUE, translation);										//--- tests, the value was return without change ---
		mockCore.Verify(x => x.GetTranslation(TestCulture, TEST_KEY), Times.Once());	//--- tests, the call was routed to the core with the specific culture and key ---

		TestConsole.WriteLine($"[✔️ PASSED] Call was routed to core with culture {B(testCultureName)} and key {B(TEST_KEY)}");
	}

	[Theory]
	[InlineData(true,	"de-DE")]
	[InlineData(true,	"en-US")]
	[InlineData(false,	"de-DE")]
	[InlineData(false,	"en-US")]
	public void GetTranslation_WithSpecificCulture_WithKeyAndParsePlaceholders_DelegatesToCore(bool parsePlaceholders, string testCultureName)
	{
		//--- ARRANGE ---------------------------------------------------------
		TestConsole.WriteLine($"Testing culture {B(testCultureName)}");

		const string TEST_KEY				= "TestKey";
		const string EXPECTED_TRANSLATION	= "TranslatedValue";
		CultureInfo TestCulture				= CultureInfo.CreateSpecificCulture(testCultureName);

		//--- setup mock core to return expected value for the specific culture, key, and parsePlaceholders flag ---
		LocalizationController sut	= CreateTestLocalizationController(TEST_CULTURE_FR, out Mock<ILocalizationCore> mockCore);
		_ = mockCore
			.Setup(x => x.GetTranslation(TestCulture, TEST_KEY, parsePlaceholders))
			.Returns(EXPECTED_TRANSLATION);

		//--- ACT -------------------------------------------------------------
		string translation = sut.GetTranslation(TestCulture, TEST_KEY, parsePlaceholders);

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(EXPECTED_TRANSLATION, translation);	//--- tests, the value was return without change ---
		mockCore.Verify(x => x.GetTranslation(TestCulture, TEST_KEY, parsePlaceholders), Times.Once());	//--- tests, the call was routed to the core with the specific culture and key ---

		TestConsole.WriteLine($"[✔️ PASSED] Call was routed to core with culture {B(testCultureName)}, key {B(TEST_KEY)}, and parsePlaceholders={B(parsePlaceholders)}");
	}

	#endregion GetTranslation Method
}
