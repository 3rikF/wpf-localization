using System.Globalization;
using System.Windows;
using System.Windows.Markup;

using ErikForwerk.Localization.WPF.Interfaces;
using ErikForwerk.Localization.WPF.Models;

using Moq;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Tests.Models;

//-----------------------------------------------------------------------------------------------------------------------------------------
public sealed class LocalizationControllerTests
{
	//-----------------------------------------------------------------------------------------------------------------
	#region Test Data

	private static readonly CultureInfo TEST_CULTURE_DE = CultureInfo.GetCultureInfo("de-DE");
	private static readonly CultureInfo TEST_CULTURE_EN = CultureInfo.GetCultureInfo("en-US");
	private static readonly CultureInfo TEST_CULTURE_FR = CultureInfo.GetCultureInfo("fr-FR");

	#endregion Test Data

	//-----------------------------------------------------------------------------------------------------------------
	#region Helper Methods

	//private static Mock<ILocalizationCore> CreateMockLocalizationCore(CultureInfo initialCulture)
	//{
	//	Mock<ILocalizationCore> mock= new();
	//
	//	return mock;
	//}

	private static LocalizationController CreateTestLocalizationController(CultureInfo initialCulture
		, out Mock<LocalizationController.IWindow> out_mockWindow
		, out Mock<ILocalizationCore> out_mockCore)
	{
		out_mockWindow	= new();
		out_mockCore	= new();
		_ = out_mockCore.SetupProperty(x => x.CurrentCulture, initialCulture);
		_ = out_mockCore.Setup(x => x.SupportedCultures).Returns([TEST_CULTURE_EN, TEST_CULTURE_DE]);
		_ = out_mockCore.Setup(x => x.AddTranslations(It.IsAny<ISingleCultureDictionary>()));

		return LocalizationController.CreateUnitTestInstance(out_mockWindow.Object, out_mockCore.Object);
	}

	#endregion Helper Methods

	//-----------------------------------------------------------------------------------------------------------------
	#region Construction

	//[Fact]
	//public void Ctor_WithWindow_InitializesSuccessfully()
	//{
	//	//--- ARRANGE ---------------------------------------------------------
	//	Window window = new();
	//
	//	//--- ACT -------------------------------------------------------------
	//	LocalizationController uut = new(window);
	//
	//	//--- ASSERT ----------------------------------------------------------
	//	Assert.NotNull(uut);
	//	Assert.NotNull(uut.CurrentCulture);
	//	Assert.NotNull(uut.SupportedCultures);
	//}

	[Fact]
	public void CreateUnitTestInstance_WithMockCore_InitializesSuccessfully()
	{
		//--- ARRANGE ---------------------------------------------------------
		//--- ACT -------------------------------------------------------------
		LocalizationController uut = CreateTestLocalizationController(TEST_CULTURE_EN, out _, out _);

		//--- ASSERT ----------------------------------------------------------
		Assert.NotNull(uut);
	}

	#endregion Construction

	//-----------------------------------------------------------------------------------------------------------------
	#region CurrentCulture Property

	[Fact]
	public void CurrentCulture_ReturnsCultureFromCore()
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationController uut = CreateTestLocalizationController(
			TEST_CULTURE_DE
			, out _
			, out Mock<ILocalizationCore> mockCore);

		//--- ACT -------------------------------------------------------------
		CultureInfo result = uut.CurrentCulture;

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(TEST_CULTURE_DE, result);
		//--- once in the constructor and once in the property get ---
		mockCore.VerifyGet(x => x.CurrentCulture, Times.Exactly(2));
	}

	[Fact]
	public void CurrentCulture_UpdatesCultureInCoreAndWindow()
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationController uut = CreateTestLocalizationController(
			TEST_CULTURE_EN
			, out var mockWindow
			, out var mockCore);
		//--- ACT -------------------------------------------------------------
		uut.CurrentCulture = TEST_CULTURE_DE;

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(TEST_CULTURE_DE, uut.CurrentCulture);

		mockCore.VerifySet(x => x.CurrentCulture = TEST_CULTURE_DE, Times.Once());
		mockWindow.VerifySet(x => x.Language = XmlLanguage.GetLanguage(TEST_CULTURE_DE.IetfLanguageTag), Times.Once());
	}

	[Fact]
	public void CurrentCulture_SameValue_DoesNotUpdate()
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationController uut = CreateTestLocalizationController(
			TEST_CULTURE_DE
			, out var mockWindow
			, out var mockCore);

		//--- ACT -------------------------------------------------------------
		uut.CurrentCulture = TEST_CULTURE_DE;

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(TEST_CULTURE_DE, uut.CurrentCulture);

		mockCore.VerifySet(x => x.CurrentCulture = It.IsAny<CultureInfo>(), Times.Never());

		//--- once in the constructor, but not in the property set ---
		mockWindow.VerifySet(x => x.Language = It.IsAny<XmlLanguage>(), Times.Once());
	}

	[Fact]
	public void CurrentCulture_SetWithWindow_UpdatesWindowLanguage()
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationController uut = CreateTestLocalizationController(
			TEST_CULTURE_EN
			, out var mockWindow
			, out var mockCore);

		//--- ACT -------------------------------------------------------------
		uut.CurrentCulture = TEST_CULTURE_DE;

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(TEST_CULTURE_DE, uut.CurrentCulture);

		mockCore.VerifySet(x => x.CurrentCulture = TEST_CULTURE_DE, Times.Once());

		//--- assert, that the mock was called once with each language ---
		mockWindow.VerifySet(x => x.Language = It.IsAny<XmlLanguage>(), Times.Exactly(2));
		mockWindow.VerifySet(x => x.Language = XmlLanguage.GetLanguage(TEST_CULTURE_EN.IetfLanguageTag), Times.Once());
		mockWindow.VerifySet(x => x.Language = XmlLanguage.GetLanguage(TEST_CULTURE_DE.IetfLanguageTag), Times.Once());
	}

	#endregion CurrentCulture Property

	//-----------------------------------------------------------------------------------------------------------------
	#region SupportedCultures Property

	[Fact]
	public void SupportedCultures_Get_ReturnsCulturesFromCore()
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationController uut = CreateTestLocalizationController(
			TEST_CULTURE_DE
			, out var mockWindow
			, out var mockCore);

		//--- ACT -------------------------------------------------------------
		IEnumerable<CultureInfo> result = uut.SupportedCultures;

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
		LocalizationController uut = CreateTestLocalizationController(
			TEST_CULTURE_DE
			, out var mockWindow
			, out var mockCore);

		SingleCultureDictionary dictionary = new(TEST_CULTURE_DE);
		dictionary.AddOrUpdate("Hello", "Hallo");

		//--- ACT -------------------------------------------------------------
		uut.AddTranslations(dictionary);

		//--- ASSERT ----------------------------------------------------------
		mockCore.Verify(x => x.AddTranslations(dictionary), Times.Once());
	}

	[Fact]
	public void AddTranslations_MultipleDictionaries_DelegatesToCoreForEach()
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationController uut = CreateTestLocalizationController(
			TEST_CULTURE_DE
			, out var mockWindow
			, out var mockCore);

		SingleCultureDictionary dict1 = new(TEST_CULTURE_DE);
		dict1.AddOrUpdate("Hello", "Hallo");

		SingleCultureDictionary dict2 = new(TEST_CULTURE_FR);
		dict2.AddOrUpdate("Hello", "Bonjour");

		//--- ACT -------------------------------------------------------------
		uut.AddTranslations(dict1);
		uut.AddTranslations(dict2);

		//--- ASSERT ----------------------------------------------------------
		mockCore.Verify(x => x.AddTranslations(It.IsAny<ISingleCultureDictionary>()), Times.Exactly(2));
		mockCore.Verify(x => x.AddTranslations(dict1), Times.Once());
		mockCore.Verify(x => x.AddTranslations(dict2), Times.Once());
	}

	#endregion AddTranslations Method

	//-----------------------------------------------------------------------------------------------------------------
	#region AddTranslationsFromCsvResource Method

	[Fact]
	public void AddTranslationsFromCsvResource_ValidResource_LoadsAndAddsDictionaries()
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationController uut = CreateTestLocalizationController(
			TEST_CULTURE_DE
			, out var mockWindow
			, out var mockCore);

		const string RESOURCE_PATH		= @"TestResources\TestTranslations.de-DE.csv";

		//--- ACT -------------------------------------------------------------
		uut.AddTranslationsFromCsvResource(RESOURCE_PATH);

		//--- ASSERT ----------------------------------------------------------
		mockCore.Verify(x => x.AddTranslations(It.IsAny<ISingleCultureDictionary>()), Times.AtLeastOnce());
	}

	#endregion AddTranslationsFromCsvResource Method

	//-----------------------------------------------------------------------------------------------------------------
	#region Integration Tests

	[Fact]
	public void Integration_CreateUnitTestInstance_AllPropertiesWork()
	{
		//--- ARRANGE ---------------------------------------------------------
		//--- ACT -------------------------------------------------------------
		LocalizationController uut = CreateTestLocalizationController(
			TEST_CULTURE_DE
			, out var mockWindow
			, out var mockCore);


		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(TEST_CULTURE_DE, uut.CurrentCulture);
		Assert.NotEmpty(uut.SupportedCultures);

		// Test setting culture
		uut.CurrentCulture = TEST_CULTURE_EN;
		Assert.Equal(TEST_CULTURE_EN, uut.CurrentCulture);

		// Test adding translations
		SingleCultureDictionary dict = new(TEST_CULTURE_FR);
		uut.AddTranslations(dict);
		mockCore.Verify(x => x.AddTranslations(dict), Times.Once());
	}

	#endregion Integration Tests
}
