using System.ComponentModel;
using System.Globalization;

using ErikForwerk.Localization.WPF.CoreLogic;
using ErikForwerk.Localization.WPF.Interfaces;
using ErikForwerk.TestAbstractions.Models;

using Moq;

using Xunit.Abstractions;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Tests.CoreLogic;

//-----------------------------------------------------------------------------------------------------------------------------------------
[Collection(nameof(TranslationCoreBindingSource))]
public sealed class TranslationCoreBindingSourceTests : TestBase, IDisposable
{
	//-----------------------------------------------------------------------------------------------------------------
	#region Construction

	public TranslationCoreBindingSourceTests(ITestOutputHelper testOutputHelper)
		: base(testOutputHelper)
	{
		TranslationCoreBindingSource.ResetInstance();
	}

	public void Dispose()
	{
		TranslationCoreBindingSource.ResetInstance();
		GC.SuppressFinalize(this);
	}

	#endregion Construction

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
		TranslationCoreBindingSource instance = TranslationCoreBindingSource.Instance;

		//--- ACT & ASSERT -----------------------------------------------
		//--- ASSERT -----------------------------------------------------
		Assert.Empty(instance.LocalizedText);
	}

	[Fact]
	public void SupportedCultures_InitiallyEmpty()
	{
		//... because we have reset it in the constructor.
		// That makes this test a little pointless, but at least it documents the initial state.

		//--- ARRANGE ---------------------------------------------------
		TranslationCoreBindingSource instance = TranslationCoreBindingSource.Instance;

		//--- ACT & ASSERT -----------------------------------------------
		//--- ASSERT -----------------------------------------------------
		Assert.Empty(instance.SupportedCultures);
	}

	#endregion Construction

	//-----------------------------------------------------------------------------------------------------------------
	#region CurrentCulture

	[Fact]
	public void CurrentCulture_InitiallyThreadCurrentUICulture()
	{
		//--- ARRANGE ---------------------------------------------------
		CultureInfo threadCulture				= Thread.CurrentThread.CurrentUICulture;
		TranslationCoreBindingSource instance	= TranslationCoreBindingSource.Instance;

		//--- ACT & ASSERT -----------------------------------------------
		//--- ASSERT -----------------------------------------------------
		Assert.Equal(threadCulture, instance.CurrentCulture);
	}

	[Fact]
	public void CurrentCulture_SetNewCulture_UpdatesThreadCurrentUICulture()
	{
		//--- ARRANGE ---------------------------------------------------
		CultureInfo newCulture					= new("fr-FR");
		TranslationCoreBindingSource instance	= TranslationCoreBindingSource.Instance;

		//--- ACT -------------------------------------------------------
		Assert.PropertyChanged(
			instance
			, nameof(TranslationCoreBindingSource.LocalizedText)
			, () => instance.CurrentCulture = newCulture);

		//--- ASSERT -----------------------------------------------------
		Assert.Equal(newCulture, instance.CurrentCulture);
		Assert.Equal(newCulture, Thread.CurrentThread.CurrentUICulture);
	}

	[Fact]
	public void CurrentCulture_SetSameCulture_DoesNotUpdate()
	{
		//--- ARRANGE ---------------------------------------------------
		CultureInfo ci					= new("fr-FR");
		TranslationCoreBindingSource uut= TranslationCoreBindingSource.Instance;

		uut.CurrentCulture				= ci;
		uut.PropertyChanged				+= FailTest;

		//--- we manually reset [Thread.CurrentThread.CurrentUICulture] to prove that it does not get changed again. ---
		Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

		//--- ACT -------------------------------------------------------
		uut.CurrentCulture = ci;

		//--- ASSERT -----------------------------------------------------
		Assert.Equal(ci, uut.CurrentCulture);
		Assert.NotEqual(ci, Thread.CurrentThread.CurrentUICulture);

		//--- otherwise the clean-up would trigger the trap ---
		uut.PropertyChanged -= FailTest;
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

		TranslationCoreBindingSource uut		= TranslationCoreBindingSource.Instance;
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
		_ = mockDict.SetupGet(m => m.Culture).Returns(testCulture);

		TranslationCoreBindingSource uut		= TranslationCoreBindingSource.Instance;

		//--- ACT & ASSERT -----------------------------------------------
		Assert.PropertyChanged(
			uut
			, nameof(TranslationCoreBindingSource.SupportedCultures)
			, () => uut.AddTranslations(mockDict.Object));
	}

	[Fact]
	public void AddTranslations_NewCulture_RaisesPropertyChangedForLocalizedText()
	{
		//--- ARRANGE ---------------------------------------------------
		CultureInfo testCulture					= new("de-DE");
		Mock<ISingleCultureDictionary> mockDict	= new();
		_ = mockDict.SetupGet(m => m.Culture).Returns(testCulture);

		TranslationCoreBindingSource uut		= TranslationCoreBindingSource.Instance;

		//--- ACT & ASSERT -----------------------------------------------
		Assert.PropertyChanged(
			uut
			, nameof(TranslationCoreBindingSource.LocalizedText)
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
		Assert.PropertyChanged(
			uut
			, nameof(TranslationCoreBindingSource.LocalizedText)
			, () => uut.AddTranslations(mockDict2.Object));
	}

	/// <summary>
	/// Tests that adding translations for a culture that is already known,
	/// will not raise [PropertyChanged] for [SupportedCultures] because no new culture was added.
	/// </summary>
	[Theory]
	[InlineData("de-DE", "de-DE", "de-DE", new string[] {"LocalizedText"})]
	[InlineData("en-US", "en-US", "en-US", new string[] {"LocalizedText"})]
	[InlineData("fr-FR", "fr-FR", "fr-FR", new string[] {"LocalizedText"})]
	[InlineData("de-DE", "es-ES", "de-DE", new string[] {"LocalizedText", "SupportedCultures"})]
	[InlineData("en-US", "es-ES", "en-US", new string[] {"LocalizedText", "SupportedCultures"})]
	[InlineData("fr-FR", "es-ES", "fr-FR", new string[] {"LocalizedText", "SupportedCultures"})]

	[InlineData("jp-JA", "de-DE", "de-DE", new string[] {})]
	[InlineData("jp-JA", "en-US", "en-US", new string[] {})]
	[InlineData("jp-JA", "fr-FR", "fr-FR", new string[] {})]

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
		void logPropertyChange(object? s, PropertyChangedEventArgs a) => _ = actualPropertyUpdates.Add(a.PropertyName);

		CultureInfo currentCulture			= CultureInfo.CreateSpecificCulture(currentCultureName);
		CultureInfo firstCulture			= CultureInfo.CreateSpecificCulture(firstDictCultureName);
		CultureInfo SecondCulture			= CultureInfo.CreateSpecificCulture(secondDictCultureName);

		Mock<ISingleCultureDictionary> mockDict1 = new();
		_ = mockDict1.SetupGet(m => m.Culture).Returns(firstCulture);

		Mock<ISingleCultureDictionary> mockDict2 = new();
		_ = mockDict2.SetupGet(m => m.Culture).Returns(SecondCulture);

		//---
		TranslationCoreBindingSource uut		= TranslationCoreBindingSource.Instance;
		//--- add first dictionary, this will sett the first and only supported culture for now ---
		uut.AddTranslations(mockDict1.Object);

		//--- set the current culture, necessary updates also depend on this ---
		uut.CurrentCulture		= currentCulture;

		//--- subscribe to property changed events to log what properties have been updated ---
		uut.PropertyChanged		+= logPropertyChange;

		//--- ACT -------------------------------------------------------------
		try
		{
			//--- add second dictionary, this is where we expect (or not expect) property changed events ---
			uut.AddTranslations(mockDict2.Object);
		}
		finally
		{
			//--- CLEANUP -----------------------------------------------------
			uut.PropertyChanged		-= logPropertyChange;
		}

		TestConsole.WriteLine($"Expected property updates: [{string.Join("], [", expectedPropertyUpdates.Order())}]");
		TestConsole.WriteLine($"Actual property updates:   [{string.Join("], [", actualPropertyUpdates.Order())}]");

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(
			expectedPropertyUpdates.Order()
			, actualPropertyUpdates.Order());
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
		TranslationCoreBindingSource instance = TranslationCoreBindingSource.Instance;

		//--- ACT -------------------------------------------------------
		string result = instance.GetTranslation(invalidKey!, parsePlaceholders);

		//--- ASSERT -----------------------------------------------------
		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public void GetTranslation_UnknownCulture_ReturnsNotTranslatedFormat()
	{
		//--- ARRANGE ---------------------------------------------------
		TranslationCoreBindingSource uut	= TranslationCoreBindingSource.Instance;
		const string TEST_KEY				= "NonExistentKey";
		const string EXPECTED_TRANSLATION = "!!!NonExistentKey!!!";

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

		TranslationCoreBindingSource uut	= TranslationCoreBindingSource.Instance;
		uut.AddTranslations(mockDict.Object);
		uut.CurrentCulture					= TEST_CI;

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

		TranslationCoreBindingSource uut	= TranslationCoreBindingSource.Instance;
		uut.AddTranslations(mockDict.Object);
		uut.CurrentCulture					= TEST_CI;

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
	public void Reset_RaisesPropertyChangedForSupportedCultures()
	{
		//--- ARRANGE ---------------------------------------------------
		TranslationCoreBindingSource uut = TranslationCoreBindingSource.Instance;

		//--- ACT & ASSERT -----------------------------------------------
		Assert.PropertyChanged(
			uut
			, nameof(TranslationCoreBindingSource.SupportedCultures)
			, () => uut.Reset());
	}

	[Fact]
	public void Reset_RaisesPropertyChanges()
	{
		//--- ARRANGE ---------------------------------------------------
		TranslationCoreBindingSource uut = TranslationCoreBindingSource.Instance;

		//--- ACT & ASSERT -----------------------------------------------
		Assert.PropertyChanged(
			uut
			, nameof(TranslationCoreBindingSource.LocalizedText)
			, uut.Reset);

		Assert.PropertyChanged(
			uut
			, nameof(TranslationCoreBindingSource.CurrentCulture)
			, uut.Reset);

		Assert.PropertyChanged(
			uut
			, nameof(TranslationCoreBindingSource.SupportedCultures)
			, uut.Reset);
	}

	[Fact]
	public void Reset_ClearsDictionariesAndResetsCulture()
	{
		//--- ARRANGE ---------------------------------------------------
		CultureInfo testCulture					= new("de-DE");
		Mock<ISingleCultureDictionary> mockDict	= new();
		_ = mockDict.SetupGet(m => m.Culture).Returns(testCulture);

		TranslationCoreBindingSource uut		= TranslationCoreBindingSource.Instance;
		uut.AddTranslations(mockDict.Object);

		CultureInfo currentThreadCulture		= Thread.CurrentThread.CurrentUICulture;

		//--- ACT -------------------------------------------------------
		uut.Reset();

		//--- ASSERT -----------------------------------------------------
		Assert.Empty(uut.SupportedCultures);
		Assert.Equal(currentThreadCulture, uut.CurrentCulture);
	}

	#endregion Reset

}
