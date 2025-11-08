
// ignore spelling: jp laceholders uut

using System.ComponentModel;
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
	public static void UpdateCalled(ITranslationChanged uut, ELocalizationChanges expectedChanges, Action action)
	{
		bool callbackCalled = false;

		uut.RegisterCallback(
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
		TranslationCoreBindingSource uut = TranslationCoreBindingSource.Instance;

		//--- ACT & ASSERT -----------------------------------------------
		//--- ASSERT -----------------------------------------------------
		Assert.Empty(uut.LocalizedText);
	}

	[Fact]
	public void SupportedCultures_InitiallyEmpty()
	{
		//... because we have reset it in the constructor.
		// That makes this test a little pointless, but at least it documents the initial state.

		//--- ARRANGE ---------------------------------------------------
		TranslationCoreBindingSource uut = TranslationCoreBindingSource.Instance;

		//--- ACT & ASSERT -----------------------------------------------
		//--- ASSERT -----------------------------------------------------
		Assert.Empty(uut.SupportedCultures);
	}

	#endregion Construction

	//-----------------------------------------------------------------------------------------------------------------
	#region CurrentCulture

	[Fact]
	public void CurrentCulture_InitiallyThreadCurrentUICulture()
	{
		//--- ARRANGE ---------------------------------------------------
		CultureInfo threadCulture			= Thread.CurrentThread.CurrentUICulture;
		TranslationCoreBindingSource uut	= TranslationCoreBindingSource.Instance;

		//--- ACT & ASSERT -----------------------------------------------
		//--- ASSERT -----------------------------------------------------
		Assert.Equal(threadCulture, uut.CurrentCulture);
	}

	[Fact]
	public void CurrentCulture_SetNewCulture_UpdatesThreadCurrentUICulture()
	{
		//--- ARRANGE ---------------------------------------------------
		CultureInfo newCulture				= new("fr-FR");
		TranslationCoreBindingSource uut	= TranslationCoreBindingSource.Instance;

		//--- ACT -------------------------------------------------------
		AssertHelper.UpdateCalled(
			uut
			, ELocalizationChanges.CurrentCulture
			, () => uut.CurrentCulture = newCulture);

		//Assert.PropertyChanged(
		//	uut
		//	, nameof(TranslationCoreBindingSource.LocalizedText)
		//	, () => uut.CurrentCulture = newCulture);

		//--- ASSERT -----------------------------------------------------
		//Assert.True(updateCalled);
		Assert.Equal(newCulture, uut.CurrentCulture);
		Assert.Equal(newCulture, Thread.CurrentThread.CurrentUICulture);
	}

	[Fact]
	public void CurrentCulture_SetSameCulture_DoesNotUpdate()
	{
		//--- ARRANGE ---------------------------------------------------
		CultureInfo ci						= new("fr-FR");
		TranslationCoreBindingSource uut	= TranslationCoreBindingSource.Instance;
		uut.CurrentCulture					= ci;
		uut.RegisterCallback(FailTest);

		//--- we manually reset [Thread.CurrentThread.CurrentUICulture] to prove that it does not get changed again. ---
		Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

		//--- ACT -------------------------------------------------------
		uut.CurrentCulture = ci;

		//--- ASSERT -----------------------------------------------------
		Assert.Equal(ci, uut.CurrentCulture);
		Assert.NotEqual(ci, Thread.CurrentThread.CurrentUICulture);

		//--- otherwise the clean-up would trigger the trap ---
		uut.UnregisterCallback(FailTest);
	}

	#endregion CurrentCulture

	//-----------------------------------------------------------------------------------------------------------------
	#region AddTranslations

	[Fact]
	public void AddTranslations_NullDictionary_ThrowsException()
	{
		//--- ARRANGE ---------------------------------------------------
		CultureInfo ci						= CultureInfo.CreateSpecificCulture("de-DE");
		TranslationCoreBindingSource uut	= TranslationCoreBindingSource.Instance;

		//--- ACT & ASSERT -----------------------------------------------
		_ = Assert.Throws<ArgumentNullException>(
			() => uut.AddTranslations(null!));
	}

	[Fact]
	public void AddTranslations_ValidData_AddAndUpdateTranslations()
	{
		//--- ARRANGE ---------------------------------------------------
		CultureInfo ci							= CultureInfo.CreateSpecificCulture("de-DE");

		//--- the first dictionary is added ---
		Mock<ISingleCultureDictionary> mockDict1= new();
		mockDict1.SetupGet(m => m.Culture).Returns(ci).Verifiable(Times.AtLeastOnce());
		mockDict1.Setup(m => m.GetAllTranslations()).Verifiable(Times.Never());
		mockDict1.Setup(m => m.AddOrUpdate(It.IsAny<ISingleCultureDictionary>())).Verifiable(Times.Once());

		//--- the second dictionary is merged with the first ---
		Mock<ISingleCultureDictionary> mockDict2= new();
		mockDict2.SetupGet(m => m.Culture).Returns(ci).Verifiable(Times.AtLeastOnce());
		mockDict2.Setup(m => m.GetAllTranslations()).Verifiable(Times.Never()); //--- it's only "never", because the first mock does not actually call [AddOrUpdate] on it.

		TranslationCoreBindingSource uut	= TranslationCoreBindingSource.Instance;
		uut.AddTranslations(mockDict1.Object);

		//--- ACT -------------------------------------------------------
		uut.AddTranslations(mockDict2.Object);

		//--- ASSERT -----------------------------------------------------
		Assert.Contains(ci, uut.SupportedCultures);
		mockDict1.VerifyAll();
		mockDict2.VerifyAll();
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

		TranslationCoreBindingSource uut		= TranslationCoreBindingSource.Instance;
		//--- ensure, that the dictionary-culture is NOT the current culture ---
		uut.CurrentCulture						= new("fr-FR");

		//--- ACT & ASSERT -----------------------------------------------
		AssertHelper.UpdateCalled(
			uut
			, ELocalizationChanges.SupportedCultures
			, () => uut.AddTranslations(mockDict.Object));
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

		TranslationCoreBindingSource uut		= TranslationCoreBindingSource.Instance;
		//--- ensure, that the dictionary-culture is also the current culture ---
		uut.CurrentCulture						= testCulture;

		//--- ACT & ASSERT -----------------------------------------------
		AssertHelper.UpdateCalled(
			uut
			, ELocalizationChanges.Translations | ELocalizationChanges.SupportedCultures
			, () => uut.AddTranslations(mockDict.Object));
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

		TranslationCoreBindingSource uut		= TranslationCoreBindingSource.Instance;
		uut.AddTranslations(mockDict1.Object);
		uut.CurrentCulture						= testCulture;

		//--- ACT & ASSERT -----------------------------------------------
		//Assert.PropertyChanged(
		//	uut
		//	, nameof(TranslationCoreBindingSource.LocalizedText)
		//	, () => uut.AddTranslations(mockDict2.Object));

		AssertHelper.UpdateCalled(
			uut
			, ELocalizationChanges.Translations
			, () => uut.AddTranslations(mockDict2.Object));
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
		TestConsole.WriteLine($"CurrentCulture:     [{currentCultureName}]");
		TestConsole.WriteLine($"FirstDictCulture:   [{firstDictCultureName}]");
		TestConsole.WriteLine($"SecondDictCulture:  [{secondDictCultureName}]");
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
		//void logPropertyChange(object? s, PropertyChangedEventArgs a) => _ = actualPropertyUpdates.Add(a.PropertyName);
		void logPropertyChange(ELocalizationChanges changes)
		{
			TestConsole.WriteLine($"PropertyChanged: [{changes}]");
			_ = actualPropertyUpdates.Add(changes.ToString());
		}

		CultureInfo currentCulture			= CultureInfo.CreateSpecificCulture(currentCultureName);
		CultureInfo firstCulture			= CultureInfo.CreateSpecificCulture(firstDictCultureName);
		CultureInfo SecondCulture			= CultureInfo.CreateSpecificCulture(secondDictCultureName);

		Mock<ISingleCultureDictionary> mockDict1 = new();
		_ = mockDict1.SetupGet(m => m.Culture).Returns(firstCulture);

		Mock<ISingleCultureDictionary> mockDict2 = new();
		_ = mockDict2.SetupGet(m => m.Culture).Returns(SecondCulture);

		TranslationCoreBindingSource uut		= TranslationCoreBindingSource.Instance;
		//--- add first dictionary, this will sett the first and only supported culture for now ---
		uut.AddTranslations(mockDict1.Object);

		//--- set the current culture, necessary updates also depend on this ---
		uut.CurrentCulture		= currentCulture;

		//--- subscribe to property changed events to log what properties have been updated ---
		//uut.PropertyChanged		+= logPropertyChange;
		uut.RegisterCallback(logPropertyChange); //--- will be cleaned up on test teardown ---

		//--- ACT -------------------------------------------------------------
		//--- add second dictionary, this is where we expect (or not expect) property changed events ---
		uut.AddTranslations(mockDict2.Object);

		TestConsole.WriteLine($"Expected property updates: [{string.Join("], [", expectedPropertyUpdates)}]");
		TestConsole.WriteLine($"Actual property updates:   [{string.Join("], [", actualPropertyUpdates)}]");

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(
			expectedPropertyUpdates
			, actualPropertyUpdates);
	}

	#endregion AddTranslations

	//-----------------------------------------------------------------------------------------------------------------
	#region GetTranslation

	[Theory]
	[InlineData(true, null)]
	[InlineData(true, "")]
	[InlineData(false, null)]
	[InlineData(false, "")]
	public void GetTranslation_WithPlaceHolders_NullOrEmptyKeyInvalidKey_ReturnsEmptyString(bool parsePlaceholders, string? invalidKey)
	{
		//--- ARRANGE ---------------------------------------------------
		TranslationCoreBindingSource uut = TranslationCoreBindingSource.Instance;

		//--- ACT -------------------------------------------------------
		string result = uut.GetTranslation(invalidKey!, parsePlaceholders);

		//--- ASSERT -----------------------------------------------------
		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public void GetTranslation_UnknownCulture_ReturnsNotTranslatedFormat()
	{
		//--- ARRANGE ---------------------------------------------------
		TranslationCoreBindingSource uut	= TranslationCoreBindingSource.Instance;
		const string TEST_KEY				= "NonExistentKey";
		const string EXPECTED_TRANSLATION	= "!!!NonExistentKey!!!";

		//--- ACT -------------------------------------------------------
		string result						= uut.GetTranslation(TEST_KEY, parsePlaceholders: false);

		//--- ASSERT -----------------------------------------------------
		Assert.Equal(EXPECTED_TRANSLATION, result);
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

		TranslationCoreBindingSource uut = TranslationCoreBindingSource.Instance;
		uut.AddTranslations(mockDict.Object);
		uut.CurrentCulture = TEST_CI;

		//--- ACT -------------------------------------------------------
		string result = uut.GetTranslation(TEST_KEY, parsePlaceholders: false);

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

		TranslationCoreBindingSource uut = TranslationCoreBindingSource.Instance;
		uut.AddTranslations(mockDict.Object);
		uut.CurrentCulture = TEST_CI;

		//--- ACT -------------------------------------------------------
		string result = uut.GetTranslation(TEST_KEY, parsePlaceholders: true);

		//--- ASSERT -----------------------------------------------------
		Assert.Equal(EXPECTED_TRANSLATION, result);
		mockDict.VerifyAll();
	}


	#endregion GetTranslation

	//-----------------------------------------------------------------------------------------------------------------
	#region Reset

	[Fact]
	public void Reset_RemovesAllPropertyChangedHandlers()
	{
		//--- ARRANGE ---------------------------------------------------
		// create a fresh test instance and subscribe handlers
		using TranslationCoreBindingSource.TestModeTracker tracker	= new();
		TranslationCoreBindingSource uut							= TranslationCoreBindingSource.Instance;

		int calls = 0;
		void fnHandler(ELocalizationChanges _) => calls++;

		uut.CurrentCulture = CultureInfo.CreateSpecificCulture("de-DE");
		uut.RegisterCallback(fnHandler);
		uut.RegisterCallback(fnHandler);

		//--- ACT -------------------------------------------------------
		// check: handlers are called
		uut.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
		Assert.Equal(1, calls);

		//--- ACT -------------------------------------------------------
		// disposing the tracker will call Reset() on the test instance
		tracker.Dispose();

		//--- ASSERT ----------------------------------------------------
		// handlers must no longer be invoked after reset
		uut.CurrentCulture = CultureInfo.InvariantCulture;
		uut.CurrentCulture = CultureInfo.CreateSpecificCulture("fr-FR");
		Assert.Equal(1, calls);
	}

	#endregion Reset
}
