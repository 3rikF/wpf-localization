
using System.Globalization;

using ErikForwerk.Localization.WPF.CoreLogic;
using ErikForwerk.Localization.WPF.Models;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Tests.CoreLogic;

//-----------------------------------------------------------------------------------------------------------------------------------------
[Collection("82A46DF4-F8CA-4E66-8606-DF49164DEFBB")]
public sealed class LocalizationTextConverterTests :IDisposable
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
	#region Test Data

	private static readonly CultureInfo TEST_CULTURE = CultureInfo.GetCultureInfo("de-DE");

	#endregion Test Data

	//-----------------------------------------------------------------------------------------------------------------
	#region Convert Tests

	[Fact]
	public void Convert_WithValidKey_ReturnsTranslation()
	{
		//--- ARRANGE ---------------------------------------------------------
		const string TEST_KEY			= "TestKey";
		const string TEST_TRANSLATION	= "Test Übersetzung";

		SingleCultureDictionary dict	= new(TEST_CULTURE);
		dict.AddOrUpdate(TEST_KEY, TEST_TRANSLATION);

		TranslationCoreBindingSource.Instance.CurrentCulture = TEST_CULTURE;
		TranslationCoreBindingSource.Instance.AddTranslations(dict);

		LocalizationTextConverter uut	= new(TEST_KEY, false);

		//--- ACT -------------------------------------------------------------
		object result = uut.Convert(null, typeof(string), null!, TEST_CULTURE);

		//--- ASSERT ----------------------------------------------------------
		_ = Assert.IsType<string>(result);
		Assert.Equal(TEST_TRANSLATION, result);
	}

	[Fact]
	public void ConvertBack_ThrowsNotSupportedException()
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationTextConverter uut = new("TestKey", false);

		//--- ACT -------------------------------------------------------------
		NotSupportedException ex = Assert.Throws<NotSupportedException>(
			() => uut.ConvertBack("SomeValue", typeof(string), null!, TEST_CULTURE));

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal("Specified method is not supported.", ex.Message);
	}

	#endregion Convert Tests
}
