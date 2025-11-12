
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;

using ErikForwerk.Localization.WPF.Interfaces;
using ErikForwerk.Localization.WPF.Reader;

using Xunit.Abstractions;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Tests.Reader;

//-----------------------------------------------------------------------------------------------------------------------------------------
public sealed class ResourceCsvReaderTests
{
	//-----------------------------------------------------------------------------------------------------------------
	#region Test Construction

	private readonly ITestOutputHelper _testConsole;

	[ExcludeFromCodeCoverage(Justification = "Untestable race condition at test run time")]
	private ResourceCsvReaderTests()
	{
		_testConsole = null!;

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

	public ResourceCsvReaderTests(ITestOutputHelper TestConsole)
		: this()
	{
		_testConsole = TestConsole;
	}

	#endregion Test Construction

	//-----------------------------------------------------------------------------------------------------------------
	#region Test Data

	public static TheoryData<string, string> ValidResourcePathsWithExpectedCulture()
	{
		return new()
		{
			{ "/TestResources/TestTranslations.de-DE.csv",	"de-DE" },
			{ "/TestResources/TestTranslations.en-US.csv",	"en-US" },
			{ "TestResources/TestTranslations.de-DE.csv",	"de-DE" },
			{ "TestResources/TestTranslations.en-US.csv",	"en-US" },
		};
	}

	[Fact]
	public void ValidResourcePathsWithExpectedCulture_HasTestData()
	{
		//--- ASSERT ----------------------------------------------------------
		Assert.NotEmpty(ValidResourcePathsWithExpectedCulture());

		_testConsole.WriteLine($"[✔️ Passed] Test data is present.");
	}

	#endregion Test Data

	//-----------------------------------------------------------------------------------------------------------------
	#region RessourceCsvReader

	[Theory]
	[MemberData(nameof(ValidResourcePathsWithExpectedCulture))]
	public void GetLocalizations_ValidResourcePath_ReturnsCorrectCultureAndContent(string resourcePath, string expectedCultureName)
	{
		//--- ARRANGE ---------------------------------------------------------
		ResourceCsvReader uut			= new(resourcePath, Assembly.GetExecutingAssembly());
		CultureInfo		expectedCulture	= CultureInfo.GetCultureInfo(expectedCultureName);

		_testConsole.WriteLine($"Testing path:     [{resourcePath}]");
		_testConsole.WriteLine($"Expected culture: [{expectedCultureName}]");

		//--- ACT -------------------------------------------------------------
		ISingleCultureDictionary[] localizations = uut.GetLocalizations();

		//--- ASSERT ----------------------------------------------------------
		Assert.NotNull(localizations);
		ISingleCultureDictionary localization = Assert.Single(localizations);
		Assert.Equal(expectedCulture, localization.Culture);

		_testConsole.WriteLine($"[✔️ Passed] Correct culture retrieved: [{localizations[0].Culture.Name}]");
	}

	[Theory]
	[MemberData(nameof(ValidResourcePathsWithExpectedCulture))]
	public void GetLocalizations_ValidResourcePath_LoadsTranslations(string resourcePath, string expectedCultureName)
	{
		//--- ARRANGE ---------------------------------------------------------
		ResourceCsvReader			uut			= new(resourcePath, Assembly.GetExecutingAssembly());
		const string				TEST_KEY1	= "TestKey1";
		const string				TEST_KEY2	= "TestKey2";

		_testConsole.WriteLine($"Testing path: [{resourcePath}]");

		//--- ACT -------------------------------------------------------------
		ISingleCultureDictionary[]	localizations	= uut.GetLocalizations();
		ISingleCultureDictionary	dictionary		= localizations[0];

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(expectedCultureName, dictionary.Culture.Name);

		Assert.True(dictionary.ContainsKey(TEST_KEY1));
		Assert.True(dictionary.ContainsKey(TEST_KEY2));

		_testConsole.WriteLine($"[✔️ Passed] Translations loaded successfully.");
	}

	[Theory]
	[InlineData("/TestResources/TestTranslations.de-DE.csv",	"TestKey1",	"TestWert1")]
	[InlineData("/TestResources/TestTranslations.en-US.csv",	"TestKey1",	"TestValue1")]
	[InlineData("/TestResources/TestTranslations.de-DE.csv",	"TestKey2",	"TestWert2")]
	[InlineData("/TestResources/TestTranslations.en-US.csv",	"TestKey2",	"TestValue2")]
	public void GetLocalizations_ValidResourcePath_ContainsExpectedTranslations(
		string resourcePath,
		string expectedKey,
		string expectedValue)
	{
		//--- ARRANGE ---------------------------------------------------------
		ResourceCsvReader uut = new(resourcePath, Assembly.GetExecutingAssembly());

		_testConsole.WriteLine($"Testing path:      [{resourcePath}]");
		_testConsole.WriteLine($"Expected [{expectedKey}] => [{expectedValue}]");

		//--- ACT -------------------------------------------------------------
		ISingleCultureDictionary[]	localizations	= uut.GetLocalizations();
		ISingleCultureDictionary	dictionary		= localizations[0];
		string						actualValue		= dictionary.GetTranslation(expectedKey);

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(expectedValue,	actualValue);
		_testConsole.WriteLine($"[✔️ Passed] Translation matches expected value.");
	}

	[Fact]
	public void GetLocalizations_ResourcePathWithoutCulture_ThrowsInvalidOperationException()
	{
		//--- ARRANGE ---------------------------------------------------------
		const string RESOURCE_PATH			= "/TestResources/InvalidNoCulture.csv";
		const string EXPECTED_MESSAGE_PART	= "IETF-language-tag";

		ResourceCsvReader uut = new(RESOURCE_PATH);

		_testConsole.WriteLine($"Testing path without culture: [{RESOURCE_PATH}]");

		//--- ACT & ASSERT ----------------------------------------------------
		InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
			() => uut.GetLocalizations());

		//--- ASSERT ----------------------------------------------------------
		Assert.Contains(EXPECTED_MESSAGE_PART,	ex.Message);
		_testConsole.WriteLine($"[✔️ Passed] InvalidOperationException thrown for missing culture.");
	}

	[Theory]
	[InlineData("/NonExistent/Resource.de-DE.csv")]
	[InlineData("/TestResources/NonExistent.en-US.csv")]
	public void GetLocalizations_NonExistentResourcePath_ThrowsFileFormatException(string resourcePath)
	{
		//--- ARRANGE ---------------------------------------------------------
		string expectedMessagePart = resourcePath.TrimStart('/').ToLower();

		ResourceCsvReader uut = new(resourcePath, Assembly.GetExecutingAssembly());

		_testConsole.WriteLine($"Testing non-existent path: [{resourcePath}]");

		//--- ACT & ASSERT ----------------------------------------------------
		IOException ex = Assert.Throws<IOException>(
			() => uut.GetLocalizations());

		//--- ASSERT ----------------------------------------------------------
		Assert.Contains(expectedMessagePart, ex.Message);
		_testConsole.WriteLine($"[✔️ Passed] FileFormatException thrown for non-existent resource.");
	}

	[Theory]
	[InlineData("/TestResources/TestTranslations.de-DE.csv",	"TestKey3")]
	[InlineData("/TestResources/TestTranslations.en-US.csv",	"TestKey3")]
	public void GetLocalizations_ResourceWithEscapedCharacters_ParsesCorrectly(string resourcePath, string keyWithEscapes)
	{
		//--- ARRANGE ---------------------------------------------------------
		ResourceCsvReader uut = new(resourcePath, Assembly.GetExecutingAssembly());

		_testConsole.WriteLine($"Testing path: [{resourcePath}]");
		_testConsole.WriteLine($"Testing key:[{keyWithEscapes}]");

		//--- ACT -------------------------------------------------------------
		ISingleCultureDictionary[]	localizations	= uut.GetLocalizations();
		string						value			= localizations[0].GetTranslation(keyWithEscapes);

		//--- ASSERT ----------------------------------------------------------
		Assert.Contains("\r\n",	value);
		Assert.Contains("\n",	value);
		_testConsole.WriteLine($"[✔️ Passed] Escaped characters parsed correctly.");
		_testConsole.WriteLine($"Value: [{value}]");
	}

	[Fact]
	public void GetLocalizations_ResourceWithEmptyContent_ThrowsFileFormatException()
	{
		//--- ARRANGE ---------------------------------------------------------
		const string RESOURCE_PATH			= "/TestResources/EmptyFile.de-DE.csv";
		const string EXPECTED_MESSAGE_PART	= "could not be found or is empty";

		ResourceCsvReader uut = new(RESOURCE_PATH, Assembly.GetExecutingAssembly());
		_testConsole.WriteLine($"Testing empty resource path: [{RESOURCE_PATH}]");

		//--- ACT & ASSERT ----------------------------------------------------
		FileFormatException ex = Assert.Throws<FileFormatException>(
			() => uut.GetLocalizations());

		//--- ASSERT ----------------------------------------------------------
		Assert.Contains(EXPECTED_MESSAGE_PART, ex.Message);
		_testConsole.WriteLine($"[✔️ Passed] FileFormatException thrown for empty resource.");
	}

	#endregion RessourceCsvReader
}
