
using System.IO;
using System.Text;

using ErikForwerk.Localization.WPF.Reader;

using Xunit.Abstractions;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Tests.Reader;

//-----------------------------------------------------------------------------------------------------------------------------------------
public sealed class CsvParserTests(ITestOutputHelper TestConsole)
{
	//-----------------------------------------------------------------------------------------------------------------
	#region Test Data

	public static TheoryData<string, int> CsvContentWithExpectedCounts()
	{
		return new()
		{
			{ string.Empty,																	0 },
			{ "key1;value1",																1 },
			{ "key1;value1\r\nkey2;value2",													2 },
			{ "// This is a comment\r\nkey1;value1\r\n// Another comment\r\nkey2;value2",	2 },
			{ "\r\n\r\nkey1;value1\r\n\r\nkey2;value2\r\n\r\n",								2 },
			{ "  key1  ;  value1  \r\nkey2;value2",											2 },
		};
	}

	public static TheoryData<string, string, string> CsvContentWithExpectedKeyValuePairs()
	{
		return new()
		{
			{ "key1;value1",																"key1",	"value1" },
			{ "key1;value1\r\nkey2;value2",													"key2",	"value2" },
			{ "// This is a comment\r\nkey1;value1\r\n// Another comment\r\nkey2;value2",	"key2",	"value2" },
			{ "  key1  ;  value1  \r\nkey2;value2",											"key1",	"value1" },
		};
	}

	#endregion Test Data

	//-----------------------------------------------------------------------------------------------------------------
	#region Meta Tests

	[Fact]
	public void CsvContentWithExpectedCounts_ContainsTestData()
	{
		//--- ASSERT ----------------------------------------------------------
		Assert.NotEmpty(CsvContentWithExpectedCounts());
		TestConsole.WriteLine($"[✔️ Passed] CsvContentWithExpectedCounts contains [{CsvContentWithExpectedCounts().Count}] entries.");
	}

	[Fact]
	public void CsvContentWithExpectedKeyValuePairs_ContainsTestData()
	{
		//--- ASSERT ----------------------------------------------------------
		Assert.NotEmpty(CsvContentWithExpectedKeyValuePairs());
		TestConsole.WriteLine($"[✔️ Passed] CsvContentWithExpectedKeyValuePairs contains [{CsvContentWithExpectedKeyValuePairs().Count}] entries.");
	}

	#endregion Meta Tests

	//-----------------------------------------------------------------------------------------------------------------
	#region ParseString

	[Fact]
	public void ParseString_NullInput_ThrowsArgumentNullException()
	{
		//--- ARRANGE ---------------------------------------------------------
		//--- ACT -------------------------------------------------------------
		Dictionary<string, string> tmp = CsvParser.ParseString(null!);

		//--- ASSERT ----------------------------------------------------------
		Assert.Empty(tmp);

		TestConsole.WriteLine("[✔️ Passed] Null input returns empty dictionary.");
	}

	[Theory]
	[MemberData(nameof(CsvContentWithExpectedCounts))]
	public void ParseString_ValidInput_ReturnsDictionaryWithCorrectCount(string csvContent, int expectedCount)
	{
		//--- ARRANGE ---------------------------------------------------------
		TestConsole.WriteLine($"Testing CSV content: \r\n[{csvContent}]");
		TestConsole.WriteLine(string.Empty);

		//--- ACT -------------------------------------------------------------
		Dictionary<string, string> result = CsvParser.ParseString(csvContent);

		TestConsole.WriteLine($"Expected Values [{expectedCount}]");
		TestConsole.WriteLine($"Actual Values   [{result.Count}]");

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(expectedCount, result.Count);
	}

	[Theory]
	[MemberData(nameof(CsvContentWithExpectedKeyValuePairs))]
	public void ParseString_ValidInput_ContainsExpectedKeyValuePairs(string csvContent, string expectedKey, string expectedValue)
	{
		//--- ARRANGE ---------------------------------------------------------
		TestConsole.WriteLine($"Testing CSV content: \r\n[{csvContent}]");
		TestConsole.WriteLine(string.Empty);

		//--- ACT -------------------------------------------------------------
		Dictionary<string, string> result = CsvParser.ParseString(csvContent);

		TestConsole.WriteLine($"Expected [{expectedKey}] => [{expectedValue}]");
		TestConsole.WriteLine($"Actual   [{expectedKey}] => [{result.GetValueOrDefault(expectedKey, "<not found>")}]");

		//--- ASSERT ----------------------------------------------------------
		Assert.True(result.ContainsKey(expectedKey));
		Assert.Equal(expectedValue, result[expectedKey]);
	}


	[Theory]
	[InlineData(@"key1;value\r\nwith\nnewlines",	true, "key1",					"value\r\nwith\nnewlines")]
	[InlineData(@"key2;value\twith\ttabs",			true, "key2",					"value\twith\ttabs")]
	[InlineData(@"key3;value\;with\;semicolon",		true, "key3",					"value;with;semicolon")]
	[InlineData(@"key\;with\;semicolon;value",		true, "key;with;semicolon",		"value")]
	[InlineData(@";valueWithMissingKey",			false, "",						"valueWithMissingKey")]			//--- missing key ---
	[InlineData("keyWithMissingValue;",				true, "keyWithMissingValue",	"")]							//--- missing value ---
	[InlineData("key1;value1;extra",				true, "key1",					"value1")]						//--- extra with superfluid values ---
	[InlineData("123",								false, "",						"")]							//--- invalid line ---
	[InlineData("blah",								false, "",						"")]							//--- invalid line ---
	[InlineData("// comment",						false, "",						"")]							//--- comment ---
	[InlineData(" // comment",						false, "",						"")]							//--- comment ---
	[InlineData("\t// comment",						false, "",						"")]							//--- comment ---
	public void ParseString_SpecialCases(string testInput, bool expectedValues, string expectedKey, string expectedValue)
	{
		//--- ACT -----------------------------------------------------------------
		Dictionary<string, string> result = CsvParser.ParseString(testInput);

		//--- ASSERT --------------------------------------------------------------

		if (expectedValues)
		{
			Assert.True(result.ContainsKey(expectedKey));
			Assert.Equal(expectedValue, result[expectedKey]);
		}
		else
		{
			Assert.Empty(result);
		}
	}


	#endregion ParseString

	//-----------------------------------------------------------------------------------------------------------------
	#region ParseStream

	[Fact]
	public void ParseStream_NullStream_ThrowsArgumentNullException()
	{
		//--- ARRANGE -------------------------------------------------------------
		Stream? stream = null;

		//--- ACT -----------------------------------------------------------------
		ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
			() => CsvParser.ParseStream(stream!));

		//--- ASSERT --------------------------------------------------------------
		Assert.Equal("stream", ex.ParamName);
		Assert.Contains("Value cannot be null.", ex.Message);
	}

	[Theory]
	[MemberData(nameof(CsvContentWithExpectedCounts))]
	public void ParseStream_ValidInput_ReturnsDictionaryWithCorrectCount(string csvContent, int expectedCount)
	{
		//--- ARRANGE -------------------------------------------------------------
		using MemoryStream stream = new(Encoding.UTF8.GetBytes(csvContent));

		//--- ACT -----------------------------------------------------------------
		Dictionary<string, string> result = CsvParser.ParseStream(stream);

		//--- ASSERT --------------------------------------------------------------
		Assert.Equal(expectedCount, result.Count);
	}

	[Theory]
	[MemberData(nameof(CsvContentWithExpectedKeyValuePairs))]
	public void ParseStream_ValidInput_ContainsExpectedKeyValuePairs(string csvContent, string expectedKey, string expectedValue)
	{
		//--- ARRANGE -------------------------------------------------------------
		using MemoryStream stream = new(Encoding.UTF8.GetBytes(csvContent));

		//--- ACT -----------------------------------------------------------------
		Dictionary<string, string> result = CsvParser.ParseStream(stream);

		//--- ASSERT --------------------------------------------------------------
		Assert.True(result.ContainsKey(expectedKey));
		Assert.Equal(expectedValue, result[expectedKey]);
	}

	#endregion ParseStream
}
