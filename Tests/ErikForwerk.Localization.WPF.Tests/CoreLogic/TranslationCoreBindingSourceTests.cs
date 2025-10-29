
using System.ComponentModel;
using System.Globalization;

using ErikForwerk.Localization.WPF.CoreLogic;
using ErikForwerk.Localization.WPF.Interfaces;

using Moq;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Tests.CoreLogic;

//-----------------------------------------------------------------------------------------------------------------------------------------
// Collection Definition für sequentielle Ausführung
[CollectionDefinition("TranslationCoreBindingSource Collection", DisableParallelization = true)]
public class TranslationCoreBindingSourceCollection
{
	//--- This class has no implementation. ---
	//--- It is only used as a marker for the collection. ---
}

//-----------------------------------------------------------------------------------------------------------------------------------------
[Collection("TranslationCoreBindingSource Collection")]
public sealed class TranslationCoreBindingSourceTests : IDisposable
{
	//-----------------------------------------------------------------------------------------------------------------
	#region Construction

	public TranslationCoreBindingSourceTests()
	{
		TranslationCoreBindingSource.Instance.Reset();
	}

	public void Dispose()
	{
		TranslationCoreBindingSource.Instance.Reset();
		GC.SuppressFinalize(this);
	}

	#endregion Construction

	//-----------------------------------------------------------------------------------------------------------------
	#region Test Helper

	private void FailTest(object? sender, PropertyChangedEventArgs args)
		=> Assert.Fail($"Raised [PropertyChanged] on [{args.PropertyName}]. This code path should not be reached.");

	#endregion Test Helper

	//-----------------------------------------------------------------------------------------------------------------
	#region Meta Tests

	[Fact]
	public void FailTests_ShouldFail()
	{
		//--- ARRANGE & ACT & ASSERT ---------------------------------------------------
		_ = Assert.Throws<Xunit.Sdk.FailException>(
			() => FailTest(null, new PropertyChangedEventArgs("Test")));
	}


	#endregion Meta Tests

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
		CultureInfo newCulture					= new CultureInfo("fr-FR");
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
		CultureInfo ci					= new CultureInfo("fr-FR");
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
		CultureInfo testCulture					= new CultureInfo("de-DE");
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
		CultureInfo testCulture					= new CultureInfo("de-DE");
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
		CultureInfo testCulture					= new CultureInfo("de-DE");
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

	[Fact]
	public void AddTranslations_KnownCultureNotMatchingCurrentCulture_DoesNotRaisePropertyChanged()
	{
		//--- ARRANGE ---------------------------------------------------
		CultureInfo testCulture					= new CultureInfo("de-DE");
		CultureInfo otherCulture				= new CultureInfo("fr-FR");

		Mock<ISingleCultureDictionary> mockDict1= new();
		_ = mockDict1.SetupGet(m => m.Culture).Returns(testCulture);
		_ = mockDict1.Setup(m => m.AddOrUpdate(It.IsAny<ISingleCultureDictionary>()));

		Mock<ISingleCultureDictionary> mockDict2= new();
		_ = mockDict2.SetupGet(m => m.Culture).Returns(testCulture);

		TranslationCoreBindingSource uut		= TranslationCoreBindingSource.Instance;
		uut.AddTranslations(mockDict1.Object);
		uut.CurrentCulture						= otherCulture;
		uut.PropertyChanged						+= FailTest;

		//--- ACT -------------------------------------------------------
		uut.AddTranslations(mockDict2.Object);

		//--- ASSERT -----------------------------------------------------
		Assert.Contains(testCulture, uut.SupportedCultures);

		//--- otherwise the clean-up would trigger the trap ---
		uut.PropertyChanged -= FailTest;
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
	public void Reset_RaisesPropertyChangedForLocalizedText()
	{
		//--- ARRANGE ---------------------------------------------------
		TranslationCoreBindingSource uut = TranslationCoreBindingSource.Instance;

		//--- ACT & ASSERT -----------------------------------------------
		Assert.PropertyChanged(
			uut
			, nameof(TranslationCoreBindingSource.LocalizedText)
			, () => uut.Reset());
	}

	[Fact]
	public void Reset_ClearsDictionariesAndResetsCulture()
	{
		//--- ARRANGE ---------------------------------------------------
		CultureInfo testCulture					= new CultureInfo("de-DE");
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
