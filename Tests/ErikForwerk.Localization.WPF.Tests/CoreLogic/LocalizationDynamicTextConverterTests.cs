
using System.ComponentModel;
using System.Globalization;
using System.Windows;

using ErikForwerk.Localization.WPF.CoreLogic;
using ErikForwerk.Localization.WPF.Models;

using Xunit.Abstractions;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Tests.CoreLogic;

//-----------------------------------------------------------------------------------------------------------------------------------------
[Collection(nameof(TranslationCoreBindingSource))]
public sealed class LocalizationDynamicTextConverterTests(ITestOutputHelper _toh)
{
	//-----------------------------------------------------------------------------------------------------------------
	#region Test Data

	private static readonly CultureInfo TEST_CULTURE = CultureInfo.GetCultureInfo("de-DE");

	public static TheoryData<string?[]> EmptyValuesData()
	{
		return [
			[],
			[null!],
			[""],
		];
	}

	#endregion Test Data

	//-----------------------------------------------------------------------------------------------------------------
	#region Meta-Tests

	[Fact]
	public void EmptyValuesData_ContainsElements()
	{
		//--- ACT & ASSERT ----------------------------------------------------
		Assert.NotEmpty(EmptyValuesData());
	}

	#endregion Meta-Tests

	//-----------------------------------------------------------------------------------------------------------------
	#region Convert Tests

	[Fact]
	public void Convert_WithValidKey_ReturnsTranslation()
	{
		//--- ARRANGE ---------------------------------------------------------
		const string TEST_KEY			= "DynamicKey";
		const string TEST_TRANSLATION	= "Dynamische Übersetzung";

		SingleCultureDictionary dict	= new(TEST_CULTURE);
		dict.AddOrUpdate(TEST_KEY, TEST_TRANSLATION);

		TranslationCoreBindingSource.Instance.CurrentCulture = TEST_CULTURE;
		TranslationCoreBindingSource.Instance.AddTranslations(dict);

		LocalizationDynamicTextConverter uut	= new();
		object[] values							= [TEST_KEY, string.Empty];

		//--- ACT -------------------------------------------------------------
		object result = uut.Convert(values, typeof(string), null!, TEST_CULTURE);

		//--- ASSERT ----------------------------------------------------------
		_ = Assert.IsType<string>(result);
		Assert.Equal(TEST_TRANSLATION, result);
	}

	[Theory]
	[MemberData(nameof(EmptyValuesData))]
	public void Convert_WithEmptyValues_ReturnsEmptyString(string?[] langKeyParam)
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationDynamicTextConverter uut	= new();

		//--- ACT -------------------------------------------------------------
		object result = uut.Convert(langKeyParam!, typeof(string), null!, TEST_CULTURE);

		//--- ASSERT ----------------------------------------------------------
		_ = Assert.IsType<string>(result);
		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public void Convert_WithPlaceholderParameter_UsesParameter()
	{
		//--- ARRANGE ---------------------------------------------------------
		const string TEST_TRANSLATION_RAW		= "Text with %InnerKey%";
		const string TEST_TRANSLATION_COOKED	= "Text with innerer Wert";
		const string INNER_KEY					= "InnerKey";
		const string INNER_TRANSLATION			= "innerer Wert";

		_toh.WriteLine($"Translating:    [{TEST_TRANSLATION_RAW}]");
		_toh.WriteLine($"With inner key: [{INNER_KEY}] => [{INNER_TRANSLATION}]");
		_toh.WriteLine("");

		SingleCultureDictionary dict = new(TEST_CULTURE);
		dict.AddOrUpdate(INNER_KEY, INNER_TRANSLATION);

		TranslationCoreBindingSource.Instance.CurrentCulture = TEST_CULTURE;
		TranslationCoreBindingSource.Instance.AddTranslations(dict);

		LocalizationDynamicTextConverter uut	= new();
		object[] values							= [TEST_TRANSLATION_RAW, string.Empty];
		PlaceholderConverterParameter parameter	= new("TestPath", true);

		//--- ACT -------------------------------------------------------------
		object result = uut.Convert(values, typeof(string), parameter, TEST_CULTURE);

		_toh.WriteLine($"Expected translation: [{TEST_TRANSLATION_COOKED}]");
		_toh.WriteLine($"Actual translation:   [{result}]");

		//--- ASSERT ----------------------------------------------------------
		_ = Assert.IsType<string>(result);
		Assert.Equal(TEST_TRANSLATION_COOKED, result);

		_toh.WriteLine("[✔️ Passed] Inner placeholder was correctly replaced.");
	}

	[Fact]
	public void Convert_WithFallback_ReturnsFormattedFallback()
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationDynamicTextConverter uut	= new ();
		object[] values							= [DependencyProperty.UnsetValue];
		const string FALLBACK					= "DesignTimeKey";
		PlaceholderConverterParameter parameter	= new(FALLBACK, false);

		//--- ACT -------------------------------------------------------------
		object result = uut.Convert(values, typeof(string), parameter, TEST_CULTURE);

		//--- ASSERT ----------------------------------------------------------
		string resultString = Assert.IsType<string>(result);
		Assert.Contains(FALLBACK,	resultString);
		Assert.StartsWith("!!!",	resultString);
		Assert.EndsWith("!!!",		resultString);
	}

	[Fact]
	public void Convert_WithUnsetFallback_ReturnsDefaultFallback()
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationDynamicTextConverter uut	= new ();
		object[] values							= [DependencyProperty.UnsetValue];

		DesignerProperties.SetIsInDesignMode(new DependencyObject(), true);

		//--- ACT -------------------------------------------------------------
		object result = uut.Convert(values, typeof(string), null!, TEST_CULTURE);

		//--- ASSERT ----------------------------------------------------------
		string resultString = Assert.IsType<string>(result);
		Assert.Contains("UnknwownBindingPath", resultString);
	}

	#endregion Convert Tests

	//-----------------------------------------------------------------------------------------------------------------
	#region ConvertBack Tests

	[Fact]
	public void ConvertBack_ThrowsNotSupportedException()
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationDynamicTextConverter uut = new();

		//--- ACT & ASSERT ----------------------------------------------------
		_ = Assert.Throws<NotSupportedException>(
			() => uut.ConvertBack("SomeValue", [typeof(string)], null!, TEST_CULTURE));
	}

	#endregion ConvertBack Tests
}
