
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;

namespace ErikForwerk.Localization.WPF.Tools;

internal static class Helper
{
	public static string FormatAsNotTranslated(this string key)
		=> string.Concat("!!!", key, "!!!");


	public static bool TryGetCultureFromResourceName(string resourcePath, [NotNullWhen(true)] out CultureInfo? out_culture)
	{
		string fileName	= Path.GetFileNameWithoutExtension(resourcePath);

		out_culture		= CultureInfo
			.GetCultures(CultureTypes.SpecificCultures)
			.FirstOrDefault(culture => fileName.Contains(culture.Name, StringComparison.OrdinalIgnoreCase));

		return out_culture is not null;
	}

}
