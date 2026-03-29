
// ignore spelling: jp laceholders sut

using System.Globalization;

using ErikForwerk.Localization.WPF.CoreLogic;
using ErikForwerk.Localization.WPF.Enums;
using ErikForwerk.Localization.WPF.Interfaces;
using ErikForwerk.TestAbstractions.Models;

using Moq;

using Xunit.Abstractions;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Tests.CoreLogic;

//--- TODO: When switching to .NEt10, replace this with static extension mehtod ---
static file class AssertHelper
{
	public static void UpdateCalled(ITranslationChanged sut, ELocalizationChanges expectedChanges, Action action)
	{
		bool callbackCalled = false;

		sut.RegisterCallback(
			p =>
			{
				Assert.Equal(expectedChanges, p);
				callbackCalled = true;
			});

		action();

		Assert.True(callbackCalled);
	}
}

//-----------------------------------------------------------------------------------------------------------------------------------------
[Collection("82A46DF4-F8CA-4E66-8606-DF49164DEFBB")]
public sealed class TranslationCoreBindingSourceTests(ITestOutputHelper testOutputHelper) : TestBase(testOutputHelper), IDisposable
{
	//-----------------------------------------------------------------------------------------------------------------
	#region Test Cleanup

	private readonly TranslationCoreBindingSource.TestModeTracker _testModetracker = new ();

	public void Dispose()
	{
		_testModetracker.Dispose();
		GC.SuppressFinalize(this);
	}

	#endregion Test Cleanup

	//-----------------------------------------------------------------------------------------------------------------
	#region Construction

	[Fact]
	public void Instance_IsSingleton()
	{
		//--- ARRANGE & ACT ---------------------------------------------------
		TranslationCoreBindingSource instance1 = TranslationCoreBindingSource.Instance;
		TranslationCoreBindingSource instance2 = TranslationCoreBindingSource.Instance;

		//--- ASSERT ------------------------------------------------------------
		Assert.Same(instance1, instance2);
	}

	[Fact]
	public void LocalizedText_ReturnsEmptyString()
	{
		//--- ARRANGE ---------------------------------------------------
		TranslationCoreBindingSource sut = TranslationCoreBindingSource.Instance;

		//--- ACT & ASSERT -----------------------------------------------
		//--- ASSERT -----------------------------------------------------
		Assert.Empty(sut.LocalizedText);
	}

	[Fact]
	public void SupportedCultures_InitiallyEmpty()
	{
		//... because we have reset it in the constructor.
		// That makes this test a little pointless, but at least it documents the initial state.

		//--- ARRANGE ---------------------------------------------------
		TranslationCoreBindingSource sut = TranslationCoreBindingSource.Instance;

		//--- ACT & ASSERT -----------------------------------------------
		//--- ASSERT -----------------------------------------------------
		Assert.Empty(sut.SupportedCultures);
	}

	#endregion Construction

	//-----------------------------------------------------------------------------------------------------------------
	#region CurrentCulture

	[Fact]
	public void CurrentCulture_InitiallyThreadCurrentUICulture()
	{
		//--- ARRANGE ---------------------------------------------------
		CultureInfo threadCulture			= Thread.CurrentThread.CurrentUICulture;
		TranslationCoreBindingSource sut	= TranslationCoreBindingSource.Instance;

		//--- ACT & ASSERT -----------------------------------------------
		Assert.Equal(threadCulture, sut.CurrentCulture);
	}

	[Fact]
	public void CurrentCulture_SetNewCulture_DoesNotChangeThreadCurrentUICulture()
	{
		//--- ARRANGE ---------------------------------------------------
		CultureInfo newCulture				= new("fr-FR");
		CultureInfo originalCulture			= Thread.CurrentThread.CurrentUICulture;
		TranslationCoreBindingSource sut	= TranslationCoreBindingSource.Instance;

		//--- ACT -------------------------------------------------------
		AssertHelper.UpdateCalled(
			sut
			, ELocalizationChanges.CurrentCulture
			, () => sut.CurrentCulture = newCulture);

		//--- ASSERT -----------------------------------------------------
		Assert.Equal(newCulture,		sut.CurrentCulture);
		Assert.Equal(originalCulture,	Thread.CurrentThread.CurrentUICulture);
	}

	[Fact]
	public void CurrentCulture_SetSameCulture_DoesNotUpdate()
	{
		//--- ARRANGE ---------------------------------------------------
		CultureInfo ci						= new("fr-FR");
		TranslationCoreBindingSource sut	= TranslationCoreBindingSource.Instance;
		sut.CurrentCulture					= ci;
		sut.RegisterCallback(FailTest);

		//--- we manually reset [Thread.CurrentThread.CurrentUICulture] to prove that it does not get changed again. ---
		Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

		//--- ACT -------------------------------------------------------
		sut.CurrentCulture = ci;

		//--- ASSERT -----------------------------------------------------
		Assert.Equal(ci, sut.CurrentCulture);
		Assert.NotEqual(ci, Thread.CurrentThread.CurrentUICulture);

		//--- otherwise the clean-up would trigger the trap ---
		sut.UnregisterCallback(FailTest);
	}

	#endregion CurrentCulture

	//-----------------------------------------------------------------------------------------------------------------
	#region AddTranslations

	[Fact]
	public void AddTranslations_NullDictionary_ThrowsException()
	{
		//--- ARRANGE ---------------------------------------------------
		TranslationCoreBindingSource sut = TranslationCoreBindingSource.Instance;

		//--- ACT & ASSERT -----------------------------------------------
		_ = Assert.Throws<ArgumentNullException>(
			() => sut.AddTranslations((ISingleCultureDictionary)null!));

		_ = Assert.Throws<ArgumentNullException>(
			() => sut.AddTranslations((IEnumerable<ISingleCultureDictionary>)null!));

		_ = Assert.Throws<ArgumentNullException>(
			() => sut.AddTranslations((ILocalizationReader)null!));
	}

	[Fact]
	public void AddTranslations_ValidData_AddAndUpdateTranslations()
	{
		//--- ARRANGE ---------------------------------------------------
		CultureInfo ci = CultureInfo.CreateSpecificCulture("de-DE");

		//--- the first dictionary is added ---
		Mock<ISingleCultureDictionary> mockDict1 = new();
		mockDict1.SetupGet(m => m.Culture).Returns(ci).Verifiable(Times.AtLeastOnce());
		mockDict1.Setup(m => m.GetAllTranslations()).Verifiable(Times.Never());
		mockDict1.Setup(m => m.AddOrUpdate(It.IsAny<ISingleCultureDictionary>())).Verifiable(Times.Once());

		//--- the second dictionary is merged with the first ---
		Mock<ISingleCultureDictionary> mockDict2 = new();
		mockDict2.SetupGet(m => m.Culture).Returns(ci).Verifiable(Times.AtLeastOnce());
		mockDict2.Setup(m => m.GetAllTranslations()).Verifiable(Times.Never()); //--- it's only "never", because the first mock does not actually call [AddOrUpdate] on it.

		TranslationCoreBindingSource sut	= TranslationCoreBindingSource.Instance;
		sut.AddTranslations(mockDict1.Object);

		//--- ACT -------------------------------------------------------
		sut.AddTranslations(mockDict2.Object);

		//--- ASSERT -----------------------------------------------------
		Assert.Contains(ci, sut.SupportedCultures);
		mockDict1.VerifyAll();
		mockDict2.VerifyAll();
	}

	[Fact]
	public void AddTranslations_MultipleDictionaries_AddAndUpdateTranslations()
	{
		//--- ARRANGE ---------------------------------------------------
		CultureInfo ci1 = CultureInfo.CreateSpecificCulture("de-DE");
		CultureInfo ci2 = CultureInfo.CreateSpecificCulture("en-US");
		
		//--- the first dictionary is added ---
		Mock<ISingleCultureDictionary> mockDict1= new();
		mockDict1.SetupGet(m => m.Culture).Returns(ci1).Verifiable(Times.AtLeastOnce());
		mockDict1.Setup(m => m.GetAllTranslations()).Verifiable(Times.Never());
		
		//--- the second dictionary is added ---
		Mock<ISingleCultureDictionary> mockDict2= new();
		mockDict2.SetupGet(m => m.Culture).Returns(ci2).Verifiable(Times.AtLeastOnce());
		mockDict2.Setup(m => m.GetAllTranslations()).Verifiable(Times.Never());
		TranslationCoreBindingSource sut	= TranslationCoreBindingSource.Instance;

		//--- ACT -------------------------------------------------------
		sut.AddTranslations([mockDict1.Object, mockDict2.Object]);

		//--- ASSERT -----------------------------------------------------
		Assert.Contains(ci1, sut.SupportedCultures);
		Assert.Contains(ci2, sut.SupportedCultures);
		mockDict1.VerifyAll();
		mockDict2.VerifyAll();
	}

	[Fact]
	public void AddTranslations_LocalizationReader_AddsAllDictionaries()
	{
		//--- ARRANGE ---------------------------------------------------
		CultureInfo ci1 = CultureInfo.CreateSpecificCulture("de-DE");
		CultureInfo ci2 = CultureInfo.CreateSpecificCulture("en-US");

		//--- the first dictionary is added ---
		Mock<ISingleCultureDictionary> mockDict1= new();
		mockDict1.SetupGet(m => m.Culture).Returns(ci1).Verifiable(Times.AtLeastOnce());
		mockDict1.Setup(m => m.GetAllTranslations()).Verifiable(Times.Never());

		//--- the second dictionary is added ---
		Mock<ISingleCultureDictionary> mockDict2= new();
		mockDict2.SetupGet(m => m.Culture).Returns(ci2).Verifiable(Times.AtLeastOnce());
		mockDict2.Setup(m => m.GetAllTranslations()).Verifiable(Times.Never());
		Mock<ILocalizationReader> mockReader = new();
		mockReader
			.Setup(m => m.GetLocalizations())
			.Returns([mockDict1.Object, mockDict2.Object])
			.Verifiable(Times.Once());

		TranslationCoreBindingSource sut = TranslationCoreBindingSource.Instance;

		//--- ACT -------------------------------------------------------
		sut.AddTranslations(mockReader.Object);

		//--- ASSERT -----------------------------------------------------
		Assert.Contains(ci1, sut.SupportedCultures);
		Assert.Contains(ci2, sut.SupportedCultures);
		mockDict1.VerifyAll();
		mockDict2.VerifyAll();
		mockReader.VerifyAll();
	}

	[Fact]
	public void AddTranslations_NewCulture_RaisesPropertyChangedForSupportedCultures()
	{
		//--- ARRANGE ---------------------------------------------------
		CultureInfo testCulture					= new("de-DE");
		Mock<ISingleCultureDictionary> mockDict	= new();
		_ = mockDict
			.SetupGet(m => m.Culture)
			.Returns(testCulture);

		TranslationCoreBindingSource sut		= TranslationCoreBindingSource.Instance;
		//--- ensure, that the dictionary-culture is NOT the current culture ---
		sut.CurrentCulture						= new("fr-FR");

		//--- ACT & ASSERT -----------------------------------------------
		AssertHelper.UpdateCalled(
			sut
			, ELocalizationChanges.SupportedCultures
			, () => sut.AddTranslations(mockDict.Object));
	}

	[Theory]
	[InlineData("de-de")]
	[InlineData("en-us")]
	[InlineData("fr-fr")]
	[InlineData("jp-ja")]
	public void AddTranslations_NewCulture_RaisesPropertyChangedForLocalizedText(string testCultureName)
	{
		//--- ARRANGE ---------------------------------------------------
		CultureInfo testCulture					= new(testCultureName);
		Mock<ISingleCultureDictionary> mockDict	= new();
		_ = mockDict.SetupGet(m => m.Culture).Returns(testCulture);

		TranslationCoreBindingSource sut		= TranslationCoreBindingSource.Instance;
		//--- ensure, that the dictionary-culture is also the current culture ---
		sut.CurrentCulture						= testCulture;

		//--- ACT & ASSERT -----------------------------------------------
		AssertHelper.UpdateCalled(
			sut
			, ELocalizationChanges.Translations | ELocalizationChanges.SupportedCultures
			, () => sut.AddTranslations(mockDict.Object));
	}

	[Fact]
	public void AddTranslations_KnownCulture_RaisesPropertyChangedForLocalizedText()
	{
		//--- ARRANGE ---------------------------------------------------
		CultureInfo testCulture					= new("de-DE");
		Mock<ISingleCultureDictionary> mockDict1= new();
		_ = mockDict1.SetupGet(m => m.Culture).Returns(testCulture);
		_ = mockDict1.Setup(m => m.AddOrUpdate(It.IsAny<ISingleCultureDictionary>()));

		Mock<ISingleCultureDictionary> mockDict2= new();
		_ = mockDict2.SetupGet(m => m.Culture).Returns(testCulture);

		TranslationCoreBindingSource sut		= TranslationCoreBindingSource.Instance;
		sut.AddTranslations(mockDict1.Object);
		sut.CurrentCulture						= testCulture;

		//--- ACT & ASSERT -----------------------------------------------
		AssertHelper.UpdateCalled(
			sut
			, ELocalizationChanges.Translations
			, () => sut.AddTranslations(mockDict2.Object));
	}

	/// <summary>
	/// Tests that adding translations for a culture that is already known,
	/// will not raise [PropertyChanged] for [SupportedCultures] because no new culture was added.
	/// </summary>
	[Theory]
	[InlineData("de-DE", "de-DE", "de-DE", new string[] {"Translations"})]
	[InlineData("en-US", "en-US", "en-US", new string[] {"Translations"})]
	[InlineData("fr-FR", "fr-FR", "fr-FR", new string[] {"Translations"})]
	[InlineData("de-DE", "es-ES", "de-DE", new string[] {"Translations, SupportedCultures"})]
	[InlineData("en-US", "es-ES", "en-US", new string[] {"Translations, SupportedCultures"})]
	[InlineData("fr-FR", "es-ES", "fr-FR", new string[] {"Translations, SupportedCultures"})]

	[InlineData("jp-JA", "de-DE", "de-DE", new string[] {"None"})]
	[InlineData("jp-JA", "en-US", "en-US", new string[] {"None"})]
	[InlineData("jp-JA", "fr-FR", "fr-FR", new string[] {"None"})]

	[InlineData("jp-JA", "es-ES", "de-DE", new string[] {"SupportedCultures"})]
	[InlineData("jp-JA", "es-ES", "en-US", new string[] {"SupportedCultures"})]
	[InlineData("jp-JA", "es-ES", "fr-FR", new string[] {"SupportedCultures"})]
	public void AddTranslations_KnownCultureNotMatchingCurrentCulture_DoesNotRaisePropertyChanged(
		string currentCultureName
		, string firstDictCultureName
		, string secondDictCultureName
		, string[] expectedPropertyUpdates)
	{
		//--- EXPLANATION -----------------------------------------------------
		TestConsole.WriteLine($"CurrentCulture      {B(currentCultureName)}");
		TestConsole.WriteLine($"FirstDictCulture    {B(firstDictCultureName)}");
		TestConsole.WriteLine($"SecondDictCulture   {B(secondDictCultureName)}");
		TestConsole.WriteLine("");

		if (currentCultureName == secondDictCultureName)
			TestConsole.WriteLine("[CurrentCulture == SecondDictCulture]	=> Localization updated required");
		else
			TestConsole.WriteLine("[CurrentCulture != SecondDictCulture]	=> Localization update NOT required");

		if (firstDictCultureName == secondDictCultureName)
			TestConsole.WriteLine("[FirstDictCulture == SecondDictCulture]	=> SupportedCultures update NOT required");
		else
			TestConsole.WriteLine("[FirstDictCulture != SecondDictCulture]	=> SupportedCultures updated required");

		TestConsole.WriteLine("");

		//--- ARRANGE ---------------------------------------------------------
		HashSet<string?> actualPropertyUpdates	= [];
		void logPropertyChange(ELocalizationChanges changes)
		{
			TestConsole.WriteLine($"PropertyChanged {B(changes)}");
			_ = actualPropertyUpdates.Add(changes.ToString());
		}

		CultureInfo currentCulture			= CultureInfo.CreateSpecificCulture(currentCultureName);
		CultureInfo firstCulture			= CultureInfo.CreateSpecificCulture(firstDictCultureName);
		CultureInfo SecondCulture			= CultureInfo.CreateSpecificCulture(secondDictCultureName);

		Mock<ISingleCultureDictionary> mockDict1 = new();
		_ = mockDict1.SetupGet(m => m.Culture).Returns(firstCulture);

		Mock<ISingleCultureDictionary> mockDict2 = new();
		_ = mockDict2.SetupGet(m => m.Culture).Returns(SecondCulture);

		TranslationCoreBindingSource sut		= TranslationCoreBindingSource.Instance;
		//--- add first dictionary, this will sett the first and only supported culture for now ---
		sut.AddTranslations(mockDict1.Object);

		//--- set the current culture, necessary updates also depend on this ---
		sut.CurrentCulture		= currentCulture;

		//--- subscribe to property changed events to log what properties have been updated ---
		sut.RegisterCallback(logPropertyChange); //--- will be cleaned up on test teardown ---

		//--- ACT -------------------------------------------------------------
		//--- add second dictionary, this is where we expect (or not expect) property changed events ---
		sut.AddTranslations(mockDict2.Object);

		TestConsole.WriteLine($"Expected property updates   {B(string.Join("], [", expectedPropertyUpdates))}");
		TestConsole.WriteLine($"Actual property updates     {B(string.Join("], [", actualPropertyUpdates))}");

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(
			expectedPropertyUpdates
			, actualPropertyUpdates);
	}

	#endregion AddTranslations

	//-----------------------------------------------------------------------------------------------------------------
	#region GetTranslation (Current Culture)

	[Theory]
	[InlineData(true, null)]
	[InlineData(true, "")]
	[InlineData(false, null)]
	[InlineData(false, "")]
	public void GetTranslation_WithPlaceHolders_NullOrEmptyKeyInvalidKey_ReturnsEmptyString(bool parsePlaceholders, string? invalidKey)
	{
		//--- ARRANGE ---------------------------------------------------
		TranslationCoreBindingSource sut = TranslationCoreBindingSource.Instance;

		//--- ACT -------------------------------------------------------
		string result = sut.GetTranslation(invalidKey!, parsePlaceholders);

		//--- ASSERT -----------------------------------------------------
		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public void GetTranslation_UnknownCulture_ReturnsNotTranslatedFormat()
	{
		//--- ARRANGE ---------------------------------------------------
		TranslationCoreBindingSource sut	= TranslationCoreBindingSource.Instance;
		const string TEST_KEY				= "NonExistentKey";
		const string EXPECTED_TRANSLATION	= "!!!NonExistentKey!!!";

		//--- ACT -------------------------------------------------------
		string resultA						= sut.GetTranslation(TEST_KEY, parsePlaceholders: false);
		string resultB						= sut.GetTranslation(TEST_KEY);

		//--- ASSERT -----------------------------------------------------
		Assert.Equal(EXPECTED_TRANSLATION, resultA);
		Assert.Equal(EXPECTED_TRANSLATION, resultB);
	}

	[Fact]
	public void GetTranslation_NoPlaceholders_RoutesToDictionary()
	{
		//--- ARRANGE ---------------------------------------------------
		const string TEST_KEY					= "TestKey";
		const string EXPECTED_TRANSLATION		= "FooBar";

		CultureInfo TEST_CI						= CultureInfo.CreateSpecificCulture("de-DE");
		Mock<ISingleCultureDictionary> mockDict	= new();
		mockDict
			.SetupGet(m => m.Culture)
			.Returns(TEST_CI)
			.Verifiable(Times.AtLeastOnce());

		mockDict
			.Setup(m => m.GetTranslation(It.Is<string>(s => s == TEST_KEY)))
			.Returns((string key) => EXPECTED_TRANSLATION)
			.Verifiable(Times.Once());

		TranslationCoreBindingSource sut = TranslationCoreBindingSource.Instance;
		sut.AddTranslations(mockDict.Object);
		sut.CurrentCulture = TEST_CI;

		//--- ACT -------------------------------------------------------
		string result = sut.GetTranslation(TEST_KEY, parsePlaceholders: false);

		//--- ASSERT -----------------------------------------------------
		Assert.Equal(EXPECTED_TRANSLATION, result);
		mockDict.VerifyAll();
	}

	[Fact]
	public void GetTranslation_ParsePlaceholders_RoutesToDictionary()
	{
		//--- ARRANGE ---------------------------------------------------
		const string TEST_KEY					= "TestKey %with% %placeholders%";
		const string EXPECTED_TRANSLATION		= "TestKey mit Platzhaltern";
		CultureInfo TEST_CI						= CultureInfo.CreateSpecificCulture("de-DE");
		Mock<ISingleCultureDictionary> mockDict	= new();
		mockDict
			.SetupGet(m => m.Culture)
			.Returns(TEST_CI)
			.Verifiable(Times.AtLeastOnce());

		mockDict
			.Setup(m => m.GetTranslation(It.Is<string>(s => s == "with")))
			.Returns((string key) => "mit")
			.Verifiable(Times.Once());

		mockDict
			.Setup(m => m.GetTranslation(It.Is<string>(s => s == "placeholders")))
			.Returns((string key) => "Platzhaltern")
			.Verifiable(Times.Once());

		TranslationCoreBindingSource sut = TranslationCoreBindingSource.Instance;
		sut.AddTranslations(mockDict.Object);
		sut.CurrentCulture = TEST_CI;

		//--- ACT -------------------------------------------------------
		string result = sut.GetTranslation(TEST_KEY, parsePlaceholders: true);

		//--- ASSERT -----------------------------------------------------
		Assert.Equal(EXPECTED_TRANSLATION, result);
		mockDict.VerifyAll();
	}

	#endregion GetTranslation (Current Culture)

	//-----------------------------------------------------------------------------------------------------------------
	#region GetTranslation (Specific Culture)

	[Theory]
	[InlineData("de-DE", "Bitte")]
	[InlineData("en-US", "Please")]	
	[InlineData("ja-JP", "お願いします")]
	public void GetTranslation_SpecificCulture_RoutesToDictionary(string cultureName, string expectedTranslation)
	{
		//--- ARRANGE ---------------------------------------------------
		TestConsole.WriteLine($"Testing culture         {B(cultureName)}");

		const string TEST_KEY	= "TestKey";
		CultureInfo TestCulture	= CultureInfo.CreateSpecificCulture(cultureName);

		Mock<ISingleCultureDictionary> mockDeDict	= new();
		mockDeDict.SetupGet(m => m.Culture).Returns(CultureInfo.CreateSpecificCulture("de-DE")).Verifiable(Times.AtMost(3));
		mockDeDict.Setup(m => m.GetTranslation(It.Is<string>(s => s == TEST_KEY))).Returns((string key) => "Bitte").Verifiable(Times.AtMostOnce());

		Mock<ISingleCultureDictionary> mockEnDict	= new();
		mockEnDict.SetupGet(m => m.Culture).Returns(CultureInfo.CreateSpecificCulture("en-US")).Verifiable(Times.AtMost(3));
		mockEnDict.Setup(m => m.GetTranslation(It.Is<string>(s => s == TEST_KEY))).Returns((string key) => "Please").Verifiable(Times.AtMostOnce());

		Mock<ISingleCultureDictionary> mockJpDict	= new();
		mockJpDict.SetupGet(m => m.Culture).Returns(CultureInfo.CreateSpecificCulture("ja-JP")).Verifiable(Times.AtMost(3));
		mockJpDict.Setup(m => m.GetTranslation(It.Is<string>(s => s == TEST_KEY))).Returns((string key) => "お願いします").Verifiable(Times.AtMostOnce());

		TranslationCoreBindingSource sut = TranslationCoreBindingSource.Instance;
		sut.AddTranslations(mockDeDict.Object);
		sut.AddTranslations(mockEnDict.Object);
		sut.AddTranslations(mockJpDict.Object);

		//--- ACT -------------------------------------------------------
		string result = sut.GetTranslation(TestCulture, TEST_KEY);

		TestConsole.WriteLine($"Expected translation    {B(expectedTranslation)}");
		TestConsole.WriteLine($"Actual translation      {B(result)}");	

		//--- ASSERT -----------------------------------------------------
		Assert.Equal(expectedTranslation, result);
		Mock.VerifyAll(mockDeDict, mockEnDict, mockJpDict);
	}

	#endregion GetTranslation (Specific Culture)

	//-----------------------------------------------------------------------------------------------------------------
	#region Reset

	[Fact]
	public void Reset_RemovesAllPropertyChangedHandlers()
	{
		//--- ARRANGE ---------------------------------------------------
		// create a fresh test instance and subscribe handlers
		using TranslationCoreBindingSource.TestModeTracker tracker	= new();
		TranslationCoreBindingSource sut							= TranslationCoreBindingSource.Instance;

		int calls = 0;
		void fnHandler(ELocalizationChanges _) => calls++;

		sut.CurrentCulture = CultureInfo.CreateSpecificCulture("de-DE");
		sut.RegisterCallback(fnHandler);
		sut.RegisterCallback(fnHandler);

		//--- ACT -------------------------------------------------------
		// check: handlers are called
		sut.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
		Assert.Equal(1, calls);

		//--- ACT -------------------------------------------------------
		// disposing the tracker will call Reset() on the test instance
		tracker.Dispose();

		//--- ASSERT ----------------------------------------------------
		// handlers must no longer be invoked after reset
		sut.CurrentCulture = CultureInfo.InvariantCulture;
		sut.CurrentCulture = CultureInfo.CreateSpecificCulture("fr-FR");
		Assert.Equal(1, calls);
	}

	#endregion Reset
}
