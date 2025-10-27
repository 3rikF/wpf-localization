
using System.Globalization;
using System.IO;

using ErikForwerk.Localization.WPF.Interfaces;
using ErikForwerk.Localization.WPF.Reader;

using Xunit.Abstractions;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Tests.Reader;

//-----------------------------------------------------------------------------------------------------------------------------------------
public sealed class FilesystemCsvReaderTests : IDisposable
{
	//-----------------------------------------------------------------------------------------------------------------
	#region Fields

	private readonly ITestOutputHelper _testConsole;
	private readonly string _testDirectory;

	#endregion Fields

	//-----------------------------------------------------------------------------------------------------------------
	#region Constructor & Disposal

	public FilesystemCsvReaderTests(ITestOutputHelper testConsole)
	{
		_testConsole = testConsole;

		//--- Create a temporary test directory ---
		_testDirectory = Path.Combine(Path.GetTempPath(), $"LocalizationTests_{Guid.NewGuid()}");
		_ = Directory.CreateDirectory(_testDirectory);

		_testConsole.WriteLine($"Created test directory: [{_testDirectory}]");
	}

	public void Dispose()
	{
		//--- Clean up test directory ---
		if (Directory.Exists(_testDirectory))
		{
			Directory.Delete(_testDirectory, recursive: true);
			_testConsole.WriteLine($"Cleaned up test directory: [{_testDirectory}]");
		}
	}

	#endregion Constructor & Disposal

	//-----------------------------------------------------------------------------------------------------------------
	#region Helper Methods

	private string CreateTestCsvFile(string? cultureName, Dictionary<string, string> translations)
	{
		string fileName = cultureName is null
			? Path.Combine(_testDirectory, $"InvalidNoCulture.csv")
			: Path.Combine(_testDirectory, $"Translations.{cultureName}.csv");

		using StreamWriter writer = new(fileName);

		foreach (KeyValuePair<string, string> kvp in translations)
			writer.WriteLine($"{kvp.Key};{kvp.Value}");

		_testConsole.WriteLine($"Created test file: [{fileName}]");
		return fileName;
	}

	#endregion Helper Methods

	//-----------------------------------------------------------------------------------------------------------------
	#region Constructor Tests

	[Fact]
	public void Ctor_NullLanguagesFolder_ThrowsArgumentNullException()
	{
		//--- ARRANGE ---------------------------------------------------------
		//--- ACT -------------------------------------------------------------
		ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
			() => _ = new FilesystemCsvReader(null!));

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal("languagesFolder", ex.ParamName);
		_testConsole.WriteLine($"[✔️ Passed] {ex.GetType().Name} thrown for null languages folder.");
	}

	[Fact]
	public void Ctor_NonExistentDirectory_ThrowsDirectoryNotFoundException()
	{
		//--- ARRANGE ---------------------------------------------------------
		string nonExistentPath = Path.Combine(_testDirectory, "NonExistent");

		_testConsole.WriteLine($"Testing with non-existent path: [{nonExistentPath}]");

		//--- ACT -------------------------------------------------------------
		DirectoryNotFoundException ex = Assert.Throws<DirectoryNotFoundException>(
			() => _ = new FilesystemCsvReader(nonExistentPath));

		//--- ASSERT ----------------------------------------------------------
		Assert.Contains("Languages directory not found", ex.Message);
		Assert.Contains(nonExistentPath, ex.Message);
		_testConsole.WriteLine($"[✔️ Passed] {ex.GetType().Name} thrown for non-existent directory.");
	}

	[Fact]
	public void Ctor_ValidDirectory_CreatesInstance()
	{
		//--- ARRANGE ---------------------------------------------------------
		_testConsole.WriteLine($"Testing with valid directory: [{_testDirectory}]");

		//--- ACT -------------------------------------------------------------
		FilesystemCsvReader uut = new(_testDirectory);

		//--- ASSERT ----------------------------------------------------------
		Assert.NotNull(uut);
		_testConsole.WriteLine($"[✔️ Passed] {nameof(FilesystemCsvReader)} instance created successfully.");
	}

	#endregion Constructor Tests

	//-----------------------------------------------------------------------------------------------------------------
	#region GetLocalizations Tests

	[Fact]
	public void GetLocalizations_EmptyDirectory_ReturnsEmptyArray()
	{
		//--- ARRANGE ---------------------------------------------------------
		FilesystemCsvReader uut = new(_testDirectory);

		_testConsole.WriteLine($"Testing with empty directory: [{_testDirectory}]");

		//--- ACT -------------------------------------------------------------
		ISingleCultureDictionary[] result = uut.GetLocalizations();

		//--- ASSERT ----------------------------------------------------------
		Assert.NotNull(result);
		Assert.Empty(result);

		_testConsole.WriteLine("[✔️ Passed] Empty array returned for directory with no CSV files.");
	}

	[Fact]
	public void GetLocalizations_SingleCsvFile_ReturnsSingleDictionary()
	{
		//--- ARRANGE ---------------------------------------------------------
		_testConsole.WriteLine("Testing with single CSV file (de-DE)");

		Dictionary<string, string> translations = new()
		{
			{ "TestKey1", "TestValue1" },
			{ "TestKey2", "TestValue2" }
		};
		_ = CreateTestCsvFile("de-DE", translations);
		FilesystemCsvReader uut = new(_testDirectory);

		//--- ACT -------------------------------------------------------------
		ISingleCultureDictionary[] result = uut.GetLocalizations();

		//--- ASSERT ----------------------------------------------------------
		Assert.NotNull(result);
		ISingleCultureDictionary dictionary = Assert.Single(result);
		Assert.Equal(CultureInfo.GetCultureInfo("de-DE"), dictionary.Culture);

		_testConsole.WriteLine("[✔️ Passed] Single dictionary returned with correct culture.");
	}

	[Fact]
	public void GetLocalizations_MultipleCsvFiles_ReturnsMultipleDictionaries()
	{
		//--- ARRANGE ---------------------------------------------------------
		_ = CreateTestCsvFile("de-DE", new(){{ "TestKey1", "TestWert1" }});
		_ = CreateTestCsvFile("en-US", new(){{ "TestKey1", "TestValue1" }});

		FilesystemCsvReader uut = new(_testDirectory);

		_testConsole.WriteLine("Testing with multiple CSV files (de-DE, en-US)");

		//--- ACT -------------------------------------------------------------
		ISingleCultureDictionary[] result = uut.GetLocalizations();

		//--- ASSERT ----------------------------------------------------------
		Assert.NotNull(result);
		Assert.Equal(2, result.Length);

		Assert.Contains(result, d => d.Culture.Name == "de-DE");
		Assert.Contains(result, d => d.Culture.Name == "en-US");
		_testConsole.WriteLine("[✔️ Passed] Multiple dictionaries returned with correct cultures.");
	}

	[Fact]
	public void GetLocalizations_CsvFileWithoutCulture_ThrowsInvalidOperationException()
	{
		//--- ARRANGE ---------------------------------------------------------
		string invalidFileName	= CreateTestCsvFile(null, new(){{ "TestKey1", "TestWert1" }});
		FilesystemCsvReader uut = new(_testDirectory);

		_testConsole.WriteLine($"Testing with CSV file without culture tag: [{invalidFileName}]");

		//--- ACT & ASSERT ----------------------------------------------------
		InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
			() => uut.GetLocalizations());

		//--- ASSERT ----------------------------------------------------------
		Assert.Contains("IETF-language-tag", ex.Message);
		_testConsole.WriteLine("[✔️ Passed] InvalidOperationException thrown for file without culture tag.");
	}

	[Theory]
	[InlineData("de-DE", "TestKey1", "TestWert1")]
	[InlineData("en-US", "TestKey1", "TestValue1")]
	[InlineData("fr-FR", "TestKey1", "TestValeur1")]
	public void GetLocalizations_ValidCsvFile_ContainsExpectedTranslations(
		string cultureName,
		string expectedKey,
		string expectedValue)
	{
		//--- ARRANGE ---------------------------------------------------------
		Dictionary<string, string> translations = new()
		{
			{ expectedKey, expectedValue }
		};

		_ = CreateTestCsvFile(cultureName, translations);

		FilesystemCsvReader uut = new(_testDirectory);

		_testConsole.WriteLine($"Testing [{cultureName}]: Expected [{expectedKey}] => [{expectedValue}]");

		//--- ACT -------------------------------------------------------------
		ISingleCultureDictionary[] result			= uut.GetLocalizations();
		ISingleCultureDictionary dictionary			= result.First(d => d.Culture.Name == cultureName);
		string actualValue							= dictionary.GetTranslation(expectedKey);

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(expectedValue, actualValue);
		_testConsole.WriteLine("[✔️ Passed] Translation matches expected value.");
	}

	[Fact]
	public void GetLocalizations_CsvWithMultipleEntries_LoadsAllTranslations()
	{
		//--- ARRANGE ---------------------------------------------------------
		Dictionary<string, string> translations = new()
		{
			{ "Key1", "Value1" },
			{ "Key2", "Value2" },
			{ "Key3", "Value3" },
			{ "Key4", "Value4" }
		};

		_ = CreateTestCsvFile("de-DE", translations);

		FilesystemCsvReader uut = new(_testDirectory);

		_testConsole.WriteLine($"Testing with {translations.Count} translations");

		//--- ACT -------------------------------------------------------------
		ISingleCultureDictionary[] result		= uut.GetLocalizations();
		ISingleCultureDictionary dictionary		= result.First();
		IReadOnlyDictionary<string, string> allTranslations = dictionary.GetAllTranslations();

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(translations.Count, allTranslations.Count);

		foreach (KeyValuePair<string, string> kvp in translations)
		{
			Assert.True(allTranslations.ContainsKey(kvp.Key));
			Assert.Equal(kvp.Value, allTranslations[kvp.Key]);
		}

		_testConsole.WriteLine("[✔️ Passed] All translations loaded correctly.");
	}

	[Fact]
	public void GetLocalizations_CsvWithEscapedCharacters_ParsesCorrectly()
	{
		//--- ARRANGE ---------------------------------------------------------
		string fileName = Path.Combine(_testDirectory, "Translations.de-DE.csv");

		using (StreamWriter writer = new(fileName))
		{
			writer.WriteLine(@"TestKey1;Value with\r\nnewlines");
			writer.WriteLine(@"TestKey2;Value with\ttabs");
			writer.WriteLine(@"TestKey3;Value with\;semicolon");
		}

		FilesystemCsvReader uut = new(_testDirectory);

		_testConsole.WriteLine("Testing CSV file with escaped characters");

		//--- ACT -------------------------------------------------------------
		ISingleCultureDictionary[] result		= uut.GetLocalizations();
		ISingleCultureDictionary dictionary		= result.First();

		//--- ASSERT ----------------------------------------------------------
		Assert.Contains("\r\n",		dictionary.GetTranslation("TestKey1"));
		Assert.Contains("\t",		dictionary.GetTranslation("TestKey2"));
		Assert.Contains(";",		dictionary.GetTranslation("TestKey3"));
		_testConsole.WriteLine("[✔️ Passed] Escaped characters parsed correctly.");
	}

	[Fact]
	public void GetLocalizations_CsvWithComments_IgnoresComments()
	{
		//--- ARRANGE ---------------------------------------------------------
		string fileName = Path.Combine(_testDirectory, "Translations.en-US.csv");

		using (StreamWriter writer = new(fileName))
		{
			writer.WriteLine("// This is a comment");
			writer.WriteLine("TestKey1;TestValue1");
			writer.WriteLine("// Another comment");
			writer.WriteLine("TestKey2;TestValue2");
		}

		FilesystemCsvReader uut = new(_testDirectory);

		_testConsole.WriteLine("Testing CSV file with comments");

		//--- ACT -------------------------------------------------------------
		ISingleCultureDictionary[] result		= uut.GetLocalizations();
		ISingleCultureDictionary dictionary		= result.First();
		IReadOnlyDictionary<string, string> allTranslations = dictionary.GetAllTranslations();

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(2, allTranslations.Count);
		Assert.True(allTranslations.ContainsKey("TestKey1"));
		Assert.True(allTranslations.ContainsKey("TestKey2"));
		_testConsole.WriteLine("[✔️ Passed] Comments ignored, only valid entries loaded.");
	}

	[Fact]
	public void GetLocalizations_CsvWithEmptyLines_IgnoresEmptyLines()
	{
		//--- ARRANGE ---------------------------------------------------------
		string fileName = Path.Combine(_testDirectory, "Translations.de-DE.csv");

		using (StreamWriter writer = new(fileName))
		{
			writer.WriteLine("");
			writer.WriteLine("TestKey1;TestValue1");
			writer.WriteLine("");
			writer.WriteLine("   ");
			writer.WriteLine("TestKey2;TestValue2");
			writer.WriteLine("");
		}

		FilesystemCsvReader uut = new(_testDirectory);

		_testConsole.WriteLine("Testing CSV file with empty lines");

		//--- ACT -------------------------------------------------------------
		ISingleCultureDictionary[] result		= uut.GetLocalizations();
		ISingleCultureDictionary dictionary		= result.First();
		IReadOnlyDictionary<string, string> allTranslations = dictionary.GetAllTranslations();

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(2, allTranslations.Count);
		_testConsole.WriteLine("[✔️ Passed] Empty lines ignored, only valid entries loaded.");
	}

	[Fact]
	public void GetLocalizations_CalledMultipleTimes_ReturnsConsistentResults()
	{
		//--- ARRANGE ---------------------------------------------------------
		Dictionary<string, string> translations = new()
		{
			{ "TestKey1", "TestValue1" },
			{ "TestKey2", "TestValue2" }
		};

		_ = CreateTestCsvFile("de-DE", translations);

		FilesystemCsvReader uut = new(_testDirectory);

		_testConsole.WriteLine("Testing multiple calls to GetLocalizations");

		//--- ACT -------------------------------------------------------------
		ISingleCultureDictionary[] result1 = uut.GetLocalizations();
		ISingleCultureDictionary[] result2 = uut.GetLocalizations();

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(result1.Length, result2.Length);
		Assert.Equal(result1[0].Culture, result2[0].Culture);

		IReadOnlyDictionary<string, string> allTranslations1 = result1[0].GetAllTranslations();
		IReadOnlyDictionary<string, string> allTranslations2 = result2[0].GetAllTranslations();

		Assert.Equal(allTranslations1.Count, allTranslations2.Count);
		_testConsole.WriteLine("[✔️ Passed] Multiple calls return consistent results.");
	}

	[Fact]
	public void GetLocalizations_CsvWithWhitespace_TrimsKeysAndValues()
	{
		//--- ARRANGE ---------------------------------------------------------
		string fileName = Path.Combine(_testDirectory, "Translations.en-US.csv");

		using (StreamWriter writer = new(fileName))
		{
			writer.WriteLine("  TestKey1  ;  TestValue1  ");
			writer.WriteLine("TestKey2;TestValue2");
		}

		FilesystemCsvReader uut = new(_testDirectory);

		_testConsole.WriteLine("Testing CSV file with whitespace around keys/values");

		//--- ACT -------------------------------------------------------------
		ISingleCultureDictionary[] result		= uut.GetLocalizations();
		ISingleCultureDictionary dictionary		= result.First();

		//--- ASSERT ----------------------------------------------------------
		string value1 = dictionary.GetTranslation("TestKey1");
		Assert.Equal("TestValue1", value1);

		_testConsole.WriteLine("[✔️ Passed] Keys and values trimmed correctly.");
	}

	#endregion GetLocalizations Tests
}
