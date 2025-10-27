
using ErikForwerk.Localization.WPF.Models;

namespace ErikForwerk.Localization.WPF.Tests.Models;

public sealed class PlaceholderConverterParameterTests
{
	[Fact]
	public void Ctor_PlaceholderConverterParameter()
	{
		//--- ARRANGE ---------------------------------------------------------
		const string designTimeFallback = "Fallback";
		const bool parsePlaceholders = true;

		//--- ACT -------------------------------------------------------------
		PlaceholderConverterParameter parameter = new(designTimeFallback, parsePlaceholders);

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(designTimeFallback, parameter.DesignTimeFallback);
		Assert.Equal(parsePlaceholders, parameter.ParsePlaceholders);
	}

	[Fact]
	public void PlaceholderConverterParameter_SetterGetter()
	{
		//--- ARRANGE ---------------------------------------------------------
		PlaceholderConverterParameter uut = new("FooBar", false);

		const string TEST_DT_FALLBACK		= "Fallback";
		const bool TEST_PARSE_PLACEHOLDERS	= true;

		//--- ACT -------------------------------------------------------------
		uut = uut with
		{
			DesignTimeFallback	= TEST_DT_FALLBACK,
			ParsePlaceholders	= TEST_PARSE_PLACEHOLDERS
		};

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(TEST_DT_FALLBACK, uut.DesignTimeFallback);
		Assert.Equal(TEST_PARSE_PLACEHOLDERS, uut.ParsePlaceholders);
	}
}
