using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;

using ErikForwerk.Localization.WPF.Tools;

using Xunit.Abstractions;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Tests.Tools;

//-----------------------------------------------------------------------------------------------------------------------------------------
public sealed class ResourceHelperTests
{
	private readonly ITestOutputHelper _testConsole;

	[ExcludeFromCodeCoverage(Justification = "Untestable race condition at test run time")]
	private ResourceHelperTests()
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

	public ResourceHelperTests(ITestOutputHelper TestConsole)
		: this()
	{
		_testConsole = TestConsole;
	}


	//-----------------------------------------------------------------------------------------------------------------
	#region GetResourceText

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("   ")]
	[InlineData("\t")]
	[InlineData("\r\n")]
	public void GetResourceText_NullOrWhitespaceInput_ThrowsException(string? resourcePath)
	{
		//--- ARRANGE ---------------------------------------------------------
		_testConsole.WriteLine($"Testing with resourcePath: [{resourcePath ?? "<null>"}]");

		//--- ACT & ASSERT ----------------------------------------------------
		ArgumentException ex = resourcePath is null
			? Assert.Throws<ArgumentNullException>(() => ResourceHelper.GetResourceText(resourcePath!))
			: Assert.Throws<ArgumentException>(() => ResourceHelper.GetResourceText(resourcePath!));

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal("resourcePath",ex.ParamName);
		_testConsole.WriteLine($"[✔️ Passed] Argument(Null)Exception thrown for null/whitespace input.");
	}

	[Theory]
	[InlineData("/TestResources/TestResource.txt",	"/TestResources/TestResource.txt")]
	[InlineData("TestResources/TestResource.txt",	"/TestResources/TestResource.txt")]

	public void GetResourceText_ValidResourcePath_ReturnsContent(string inputPath, string expectedNormalizedPath)
	{
		//--- ARRANGE ---------------------------------------------------------
		_testConsole.WriteLine($"Testing with path:   [{inputPath}]");
		_testConsole.WriteLine($"Expected normalized: [{expectedNormalizedPath}]");

		//--- ACT -------------------------------------------------------------
		string? result = ResourceHelper.GetResourceText(inputPath, typeof(ResourceHelperTests).Assembly);

		//--- ASSERT ----------------------------------------------------------
		Assert.NotNull(result);
		Assert.NotEmpty(result);

		Assert.Contains("This is a test resource file", result);
		_testConsole.WriteLine($"[✔️ Passed] Resource content retrieved successfully.");
		_testConsole.WriteLine($"Content length: [{result.Length}] characters");
	}

	[Theory]
	[InlineData("/NonExistent/Resource.txt")]
	[InlineData("/TestResources/NonExistent.txt")]
	[InlineData("/Invalid/Path/To/Resource.xyz")]
	public void GetResourceText_InvalidResourcePath_ThrowsResourceReferenceKeyNotFoundException(string resourcePath)
	{
		//--- ARRANGE ---------------------------------------------------------
		string expectedMessageSubstring = resourcePath.TrimStart('/').ToLower();

		_testConsole.WriteLine($"Testing with invalid path: [{resourcePath}]");

		//--- ACT & ASSERT ----------------------------------------------------
		IOException ex = Assert.Throws<IOException>(
			() => ResourceHelper.GetResourceText(resourcePath));

		Assert.Contains(expectedMessageSubstring, ex.Message);
		_testConsole.WriteLine($"[✔️ Passed] ResourceReferenceKeyNotFoundException thrown for invalid path.");
	}

	[Fact]
	public void GetResourceText_MultiLineContent_ReturnsCompleteContent()
	{
		//--- ARRANGE ---------------------------------------------------------
		const string RESOURCE_PATH			= "/TestResources/TestResource.txt";
		const string EXPECTED_LINE_1		= "This is a test resource file";
		const string EXPECTED_LINE_2		= "It contains multiple lines";
		const string EXPECTED_LINE_3		= "Third line here";

		_testConsole.WriteLine($"Testing multi-line content from: [{RESOURCE_PATH}]");

		//--- ACT -------------------------------------------------------------
		string? result = ResourceHelper.GetResourceText(RESOURCE_PATH, typeof(ResourceHelperTests).Assembly);

		//--- ASSERT ----------------------------------------------------------
		Assert.NotNull(result);
		Assert.Contains(EXPECTED_LINE_1,	result);
		Assert.Contains(EXPECTED_LINE_2,	result);
		Assert.Contains(EXPECTED_LINE_3,	result);
		_testConsole.WriteLine($"[✔️ Passed] All expected lines found in multi-line content.");
		_testConsole.WriteLine($"Content:\n{result}");
	}

	#endregion GetResourceText
}
