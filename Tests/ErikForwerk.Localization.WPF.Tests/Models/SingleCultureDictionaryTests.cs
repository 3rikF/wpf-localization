
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
		SingleCultureDictionary sut = new (TEST_CULTURE);

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(TEST_CULTURE, sut.Culture);
	}

	#endregion Construction

	//-----------------------------------------------------------------------------------------------------------------
	#region AddOrUpdate Key/Value

	[Fact]
	public void AddOrUpdate_NullKey_ThrowsException()
	{
		//--- ARRANGE ---------------------------------------------------------
		SingleCultureDictionary sut = new (TEST_CULTURE);

		//--- ACT -------------------------------------------------------------
		ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
			() => sut.AddOrUpdate(null!, "SomeTranslation"));

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal("key", ex.ParamName);
	}

	[Fact]
	public void AddOrUpdate_NullValue_InsertsEmptyString()
	{
		//--- ARRANGE ---------------------------------------------------------
		SingleCultureDictionary sut = new(CultureInfo.InvariantCulture);

		//--- ACT -------------------------------------------------------------
		sut.AddOrUpdate("key", null!);

		//--- ASSERT ----------------------------------------------------------
		string result = sut.GetTranslation("key");
		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public void AddOrUpdate_ValidKeyAndTranslation_AddsTranslation()
	{
		//--- ARRANGE ---------------------------------------------------------
		SingleCultureDictionary sut		= new (TEST_CULTURE);
		const string TEST_KEY			= "Hello";
		const string TEST_TRANSLATION	= "Hello World";

		//--- ACT -------------------------------------------------------------
		sut.AddOrUpdate(TEST_KEY, TEST_TRANSLATION);

		//--- ASSERT ----------------------------------------------------------
		string retrievedTranslation = sut.GetTranslation(TEST_KEY);
		Assert.Equal(TEST_TRANSLATION, retrievedTranslation);
	}

	#endregion AddOrUpdate Key/Value

	//-----------------------------------------------------------------------------------------------------------------
	#region Array-Initialization / Add

	[Fact]
	public void ArrayInitialization_NullKey_ThrowsException()
	{
		//--- ACT -------------------------------------------------------------
		ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
			{
				_ = new SingleCultureDictionary(TEST_CULTURE)
				{
					{null!, "SomeTranslation"},
				};
			});

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal("key", ex.ParamName);
	}

	[Fact]
	public void ArrayInitialization_MultipleSameKeys_ThrowsException()
	{
		//--- ARRANGE ---------------------------------------------------------
		const string EXPECTED_MESSAGE	= "An element with the key 'Hello' already exists.";
		const string TEST_KEY			= "Hello";

		//--- ACT -------------------------------------------------------------
		ArgumentException ex = Assert.Throws<ArgumentException>(() =>
			{
				_ = new SingleCultureDictionary(TEST_CULTURE)
				{
					{ TEST_KEY, "Hello World" },
					{ TEST_KEY, "Hallo Welt" },
				};
			});

		//--- ASSERT ----------------------------------------------------------
		Assert.Contains(EXPECTED_MESSAGE, ex.Message);
	}

	[Fact]
	public void ArrayInitialization_NullValue_InsertsEmptyString()
	{
		//--- ARRANGE ---------------------------------------------------------
		const string TEST_KEY		= "The Metal Idol";
		SingleCultureDictionary sut	= 

		//--- ACT -------------------------------------------------------------
		new (CultureInfo.InvariantCulture)
		{
			{TEST_KEY, null!},
		};

		//--- ASSERT ----------------------------------------------------------
		string result = sut.GetTranslation(TEST_KEY);
		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public void ArrayInitialization_ValidKeyAndTranslation_AddsTranslation()
	{
		//--- ARRANGE ---------------------------------------------------------
		const string TEST_KEY			= "Hello";
		const string TEST_TRANSLATION	= "Hello World";

		SingleCultureDictionary sut		= 

		//--- ACT -------------------------------------------------------------
		new (TEST_CULTURE)
		{ 
			{ TEST_KEY, TEST_TRANSLATION } 
		};

		//--- ASSERT ----------------------------------------------------------
		string retrievedTranslation = sut.GetTranslation(TEST_KEY);
		Assert.Equal(TEST_TRANSLATION, retrievedTranslation);
	}

	#endregion Array-Initialization / Add

	//-----------------------------------------------------------------------------------------------------------------
	#region AddOrUpdate Other Dictionary

	[Fact]
	public void AddOrUpdate_NullDict_ThrowsException()
	{
		//--- ARRANGE ---------------------------------------------------------
		SingleCultureDictionary sut = new (TEST_CULTURE);

		//--- ACT -------------------------------------------------------------
		ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
			() => sut.AddOrUpdate(null!));

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal("otherDict", ex.ParamName);
	}

	[Fact]
	public void AddOrUpdate_CultureMismatch_ThrowsException()
	{
		//--- ARRANGE ---------------------------------------------------------
		SingleCultureDictionary mainDictionary			= new (TEST_CULTURE);
		SingleCultureDictionary additionalDictionary	= new (CultureInfo.GetCultureInfo("fr-FR"));

		Assert.NotEqual(mainDictionary.Culture, additionalDictionary.Culture);

		//--- ACT -------------------------------------------------------------
		ArgumentException ex = Assert.Throws<ArgumentException>(
			() => mainDictionary.AddOrUpdate(additionalDictionary));

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal("otherDict", ex.ParamName);
		Assert.Contains("The provided dictionary contains a different culture.", ex.Message);
	}

	[Fact]
	public void AddOrUpdate_ValidDict_AddOrUpdatesTranslations()
	{
		//--- ARRANGE ---------------------------------------------------------
		SingleCultureDictionary sut			= new (TEST_CULTURE);

		SingleCultureDictionary otherDict	= new (TEST_CULTURE);
		const string TEST_KEY_1				= "Hello";
		const string TEST_TRANSLATION_1		= "Hallo";
		const string TEST_KEY_2				= "Goodbye";
		const string TEST_TRANSLATION_2		= "Auf Wiedersehen";

		//--- new values ---
		otherDict.AddOrUpdate(TEST_KEY_1, TEST_TRANSLATION_1);
		otherDict.AddOrUpdate(TEST_KEY_2, TEST_TRANSLATION_2);

		//--- an old value that should be updated/overwritten ---
		sut.AddOrUpdate(TEST_KEY_2, "Tschüssikowski");

		//--- ACT -------------------------------------------------------------
		sut.AddOrUpdate(otherDict);

		//--- ASSERT ----------------------------------------------------------
		string retrievedTranslation1		= sut.GetTranslation(TEST_KEY_1);
		Assert.Equal(TEST_TRANSLATION_1,	retrievedTranslation1);

		string retrievedTranslation2		= sut.GetTranslation(TEST_KEY_2);
		Assert.Equal(TEST_TRANSLATION_2,	retrievedTranslation2);
	}

	#endregion AddOrUpdate Other Dictionary

	//-----------------------------------------------------------------------------------------------------------------
	#region TryGet

	[Fact]
	public void ContainsKey_NullKey_ThrowsException()
	{
		//--- ARRANGE ---------------------------------------------------------
		SingleCultureDictionary sut = new (TEST_CULTURE);

		//--- ACT -------------------------------------------------------------
		ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
			() => sut.ContainsKey(null!));

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal("key", ex.ParamName);
	}

	[Fact]
	public void ContainsKey_KeyExists_ReturnsTrueAndTranslation()
	{
		//--- ARRANGE ---------------------------------------------------------
		SingleCultureDictionary sut	= new (TEST_CULTURE);

		sut.AddOrUpdate("Hello", "Hallo");

		//--- ACT -------------------------------------------------------------
		bool notFound		= sut.ContainsKey("Goodbye");

		bool found			= sut.ContainsKey("Hello");
		string translation	= sut.GetTranslation("Hello");

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
		SingleCultureDictionary sut = new (TEST_CULTURE);

		//--- ACT -------------------------------------------------------------
		ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
			() => sut.GetTranslation(null!));

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal("key", ex.ParamName);
	}

	[Fact]
	public void GetTranslation_KeyExists_ReturnsTranslation()
	{
		//--- ARRANGE ---------------------------------------------------------
		SingleCultureDictionary sut	= new (TEST_CULTURE);
		sut.AddOrUpdate("Hello", "Hallo");

		//--- ACT -------------------------------------------------------------
		string translationExists = sut.GetTranslation("Hello");

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal("Hallo", translationExists);
	}

	[Fact]
	public void GetTranslation_KeyDoesNotExist_ReturnsMarkedKey()
	{
		//--- ARRANGE ---------------------------------------------------------
		SingleCultureDictionary sut	= new (TEST_CULTURE);

		//--- ACT -------------------------------------------------------------
		string translationNotExists	= sut.GetTranslation("Goodbye");

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
		const string TEST_KEY_1			= "Hello";
		const string TEST_TRANSLATION_1	= "Hallo";
		const string TEST_KEY_2			= "Goodbye";
		const string TEST_TRANSLATION_2	= "Auf Wiedersehen";

		SingleCultureDictionary sut		= new (TEST_CULTURE)
		{
			{TEST_KEY_1, TEST_TRANSLATION_1},
			{TEST_KEY_2, TEST_TRANSLATION_2},
		};

		//--- ACT -------------------------------------------------------------
		IReadOnlyDictionary<string, string> allTranslations = sut.GetAllTranslations();

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(2, allTranslations.Count);
		Assert.Equal(TEST_TRANSLATION_1, allTranslations[TEST_KEY_1]);
		Assert.Equal(TEST_TRANSLATION_2, allTranslations[TEST_KEY_2]);
	}

	#endregion GetAllTranslations
}
