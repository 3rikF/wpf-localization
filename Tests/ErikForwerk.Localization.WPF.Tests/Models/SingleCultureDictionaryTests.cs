
// ignore spelling: Tschüssikowski

using System.Globalization;

using ErikForwerk.Localization.WPF.Models;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Tests.Models;

//-----------------------------------------------------------------------------------------------------------------------------------------
[Collection("82A46DF4-F8CA-4E66-8606-DF49164DEFBB")]
public sealed class SingleCultureDictionaryTests
{
	//-----------------------------------------------------------------------------------------------------------------
	#region Test Data

	private	static readonly CultureInfo TEST_CULTURE = CultureInfo.GetCultureInfo("de-DE");

	#endregion Test Data

	//-----------------------------------------------------------------------------------------------------------------
	#region Construction

	[Fact]
	public void Ctor_NullCulture_ThrowsArgumentNullException()
	{
		//--- ACT -------------------------------------------------------------
		ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
			() => _ = new SingleCultureDictionary(null!));

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal("culture", ex.ParamName);
	}

	[Fact]
	public void Ctor_ValidCulture_SetsCultureProperty()
	{
		//--- ARRANGE ---------------------------------------------------------
		//--- ACT -------------------------------------------------------------
		SingleCultureDictionary dict = new (TEST_CULTURE);

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(TEST_CULTURE, dict.Culture);
	}

	#endregion Construction

	//-----------------------------------------------------------------------------------------------------------------
	#region AddOrUpdate Key/Value

	[Fact]
	public void AddOrUpdate_NullKey_ThrowsException()
	{
		//--- ARRANGE ---------------------------------------------------------
		SingleCultureDictionary dict = new (TEST_CULTURE);

		//--- ACT -------------------------------------------------------------
		ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
			() => dict.AddOrUpdate(null!, "SomeTranslation"));

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal("key", ex.ParamName);
	}

	[Fact]
	public void AddOrUpdate_NullValue_InsertsEmptyString()
	{
		//--- ARRANGE ---------------------------------------------------------
		SingleCultureDictionary dictionary = new(CultureInfo.InvariantCulture);

		//--- ACT -------------------------------------------------------------
		dictionary.AddOrUpdate("key", null!);

		//--- ASSERT ----------------------------------------------------------
		string result = dictionary.GetTranslation("key");
		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public void AddOrUpdate_ValidKeyAndTranslation_AddsTranslation()
	{
		//--- ARRANGE ---------------------------------------------------------
		SingleCultureDictionary dict	= new (TEST_CULTURE);
		const string TEST_KEY			= "Hello";
		const string TEST_TRANSLATION	= "Hello World";

		//--- ACT -------------------------------------------------------------
		dict.AddOrUpdate(TEST_KEY, TEST_TRANSLATION);

		//--- ASSERT ----------------------------------------------------------
		string retrievedTranslation = dict.GetTranslation(TEST_KEY);
		Assert.Equal(TEST_TRANSLATION, retrievedTranslation);
	}

	#endregion AddOrUpdate Key/Value

	//-----------------------------------------------------------------------------------------------------------------
	#region AddOrUpdate Other Dictionary

	[Fact]
	public void AddOrUpdate_NullDict_ThrowsException()
	{
		//--- ARRANGE ---------------------------------------------------------
		SingleCultureDictionary dict = new (TEST_CULTURE);

		//--- ACT -------------------------------------------------------------
		ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
			() => dict.AddOrUpdate(null!));

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal("otherDict", ex.ParamName);
	}

	[Fact]
	public void AddOrUpdate_CultureMismatch_ThrowsException()
	{
		//--- ARRANGE ---------------------------------------------------------
		SingleCultureDictionary dict		= new (TEST_CULTURE);
		SingleCultureDictionary otherDict	= new (CultureInfo.GetCultureInfo("fr-FR"));

		Assert.NotEqual(dict.Culture, otherDict.Culture);

		//--- ACT -------------------------------------------------------------
		ArgumentException ex = Assert.Throws<ArgumentException>(
			() => dict.AddOrUpdate(otherDict));

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal("otherDict", ex.ParamName);
		Assert.Contains("The provided dictionary has a different culture.", ex.Message);
	}

	[Fact]
	public void AddOrUpdate_ValidDict_AddOrUpdatesTranslations()
	{
		//--- ARRANGE ---------------------------------------------------------
		SingleCultureDictionary uut			= new (TEST_CULTURE);

		SingleCultureDictionary otherDict	= new (TEST_CULTURE);
		const string TEST_KEY_1				= "Hello";
		const string TEST_TRANSLATION_1		= "Hallo";
		const string TEST_KEY_2				= "Goodbye";
		const string TEST_TRANSLATION_2		= "Auf Wiedersehen";

		//--- new values ---
		otherDict.AddOrUpdate(TEST_KEY_1, TEST_TRANSLATION_1);
		otherDict.AddOrUpdate(TEST_KEY_2, TEST_TRANSLATION_2);

		//--- an old value that should be updated/overwritten ---
		uut.AddOrUpdate(TEST_KEY_2, "Tschüssikowski");

		//--- ACT -------------------------------------------------------------
		uut.AddOrUpdate(otherDict);

		//--- ASSERT ----------------------------------------------------------
		string retrievedTranslation1		= uut.GetTranslation(TEST_KEY_1);
		Assert.Equal(TEST_TRANSLATION_1,	retrievedTranslation1);

		string retrievedTranslation2		= uut.GetTranslation(TEST_KEY_2);
		Assert.Equal(TEST_TRANSLATION_2,	retrievedTranslation2);
	}

	#endregion AddOrUpdate Other Dictionary

	//-----------------------------------------------------------------------------------------------------------------
	#region TryGet

	[Fact]
	public void ContainsKey_NullKey_ThrowsException()
	{
		//--- ARRANGE ---------------------------------------------------------
		SingleCultureDictionary dict = new (TEST_CULTURE);

		//--- ACT -------------------------------------------------------------
		ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
			() => dict.ContainsKey(null!));

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal("key", ex.ParamName);
	}

	[Fact]
	public void ContainsKey_KeyExists_ReturnsTrueAndTranslation()
	{
		//--- ARRANGE ---------------------------------------------------------
		SingleCultureDictionary uut	= new (TEST_CULTURE);

		uut.AddOrUpdate("Hello", "Hallo");

		//--- ACT -------------------------------------------------------------
		bool notFound		= uut.ContainsKey("Goodbye");

		bool found			= uut.ContainsKey("Hello");
		string translation	= uut.GetTranslation("Hello");

		//--- ASSERT ----------------------------------------------------------
		Assert.False(notFound);
		Assert.True(found);

		Assert.Equal("Hallo", translation);
	}

	#endregion TryGet

	//-----------------------------------------------------------------------------------------------------------------
	#region GetTranslation

	[Fact]
	public void GetTranslation_NullKey_ThrowsException()
	{
		//--- ARRANGE ---------------------------------------------------------
		SingleCultureDictionary dict = new (TEST_CULTURE);

		//--- ACT -------------------------------------------------------------
		ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
			() => dict.GetTranslation(null!));

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal("key", ex.ParamName);
	}

	[Fact]
	public void GetTranslation_KeyExists_ReturnsTranslation()
	{
		//--- ARRANGE ---------------------------------------------------------
		SingleCultureDictionary uut	= new (TEST_CULTURE);
		uut.AddOrUpdate("Hello", "Hallo");

		//--- ACT -------------------------------------------------------------
		string translationExists	= uut.GetTranslation("Hello");

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal("Hallo", translationExists);
	}

	[Fact]
	public void GetTranslation_KeyDoesNotExist_ReturnsMarkedKey()
	{
		//--- ARRANGE ---------------------------------------------------------
		SingleCultureDictionary uut	= new (TEST_CULTURE);

		//--- ACT -------------------------------------------------------------
		string translationNotExists	= uut.GetTranslation("Goodbye");

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal("!!!Goodbye!!!", translationNotExists);
	}

	#endregion GetTranslation

	//-----------------------------------------------------------------------------------------------------------------
	#region GetAllTranslations

	[Fact]
	public void GetAllTranslations_ReturnsAllAddedTranslations()
	{
		//--- ARRANGE ---------------------------------------------------------
		SingleCultureDictionary uut	= new (TEST_CULTURE);
		const string TEST_KEY_1			= "Hello";
		const string TEST_TRANSLATION_1	= "Hallo";
		const string TEST_KEY_2			= "Goodbye";
		const string TEST_TRANSLATION_2	= "Auf Wiedersehen";
		uut.AddOrUpdate(TEST_KEY_1, TEST_TRANSLATION_1);
		uut.AddOrUpdate(TEST_KEY_2, TEST_TRANSLATION_2);

		//--- ACT -------------------------------------------------------------
		IReadOnlyDictionary<string, string> allTranslations = uut.GetAllTranslations();

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(2, allTranslations.Count);
		Assert.Equal(TEST_TRANSLATION_1, allTranslations[TEST_KEY_1]);
		Assert.Equal(TEST_TRANSLATION_2, allTranslations[TEST_KEY_2]);
	}

	#endregion GetAllTranslations
}
