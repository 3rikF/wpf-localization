using ErikForwerk.Localization.WPF.Tools;

using Xunit.Abstractions;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Tests.Tools;

//-----------------------------------------------------------------------------------------------------------------------------------------
public sealed class HelperTests
{
	private readonly ITestOutputHelper _testConsole;

	public HelperTests(ITestOutputHelper TestConsole)
	{
		_testConsole = TestConsole;
	}

	//-----------------------------------------------------------------------------------------------------------------
	#region FormatAsNotTranslated

	[Theory]
	[InlineData("Key",				"!!!Key!!!")]
	[InlineData("Key.With.Dots",	"!!!Key.With.Dots!!!")]
	[InlineData("Key With Spaces",	"!!!Key With Spaces!!!")]
	[InlineData("!!!WeirdKey!!!",	"!!!!!!WeirdKey!!!!!!")]
	[InlineData(null,				"!!!!!!")]
	[InlineData("",					"!!!!!!")]
	[InlineData(" ",				"!!! !!!")]
	[InlineData("123",				"!!!123!!!")]
	public void FormatAsNotTranslated_ValidInput_ReturnsFormattedString(string? input, string expected)
	{
		//--- ARRANGE ---------------------------------------------------------
		_testConsole.WriteLine($"Testing with input: [{input}]");
		_testConsole.WriteLine($"Expected output:    [{expected}]");

		//--- ACT -------------------------------------------------------------
		string result = input!.FormatAsNotTranslated();

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(expected,	result);
		_testConsole.WriteLine($"[✔️ Passed] Input correctly formatted as not translated.");
	}

	#endregion FormatAsNotTranslated
}
