
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;

using ErikForwerk.Localization.WPF.Interfaces;
using ErikForwerk.Localization.WPF.Models;
using ErikForwerk.Localization.WPF.Tools;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Reader;

//-----------------------------------------------------------------------------------------------------------------------------------------
/// <summary>
/// Reader implementation for loading localization CSV files from embedded resources
/// </summary>
public sealed class ResourceCsvReader : ILocalizationReader
{
	//-----------------------------------------------------------------------------------------------------------------
	#region Fields

	private readonly string _resourcePath;
	private readonly Assembly? _assembly;

	#endregion Fields

	//-----------------------------------------------------------------------------------------------------------------
	#region Constructor

	/// <summary>
	/// Initializes a new instance of the <see cref="ResourceCsvReader"/> class for reading CSV data from an embedded resource.
	/// </summary>
	/// <param name="resourcePath">
	/// The path to the embedded resource containing the CSV data.
	/// This should be a fully qualified resource name within the assembly.
	/// </param>
	/// <param name="assembly">
	/// The assembly in which to search for the embedded resource.
	/// If null, the executing assembly is used.
	/// </param>
	public ResourceCsvReader(string resourcePath, Assembly? assembly=null)
	{
		_resourcePath	= resourcePath;
		_assembly		= assembly;
	}

	#endregion Constructor

	//-----------------------------------------------------------------------------------------------------------------
	#region ILocalizationReader

	public ISingleCultureDictionary[] GetLocalizations()
	{
		if (!Helper.TryGetCultureFromResourceName(_resourcePath, out CultureInfo? csvCulture))
			throw new InvalidOperationException($"The resource name must contain a IETF-language-tag (like de-DE).");

		ReadOnlySpan<char> csvContent = ResourceHelper.GetResourceText(_resourcePath, _assembly);
		if (csvContent.IsEmpty)
			throw new FileFormatException($"The resource at path [{_resourcePath}] could not be found or is empty.");

		return [LoadLanguageContent(csvContent, csvCulture)];
	}

	#endregion ILocalizationReader

	//-----------------------------------------------------------------------------------------------------------------
	#region Private Methods

	private static SingleCultureDictionary LoadLanguageContent(ReadOnlySpan<char> content, CultureInfo culture)
	{
		SingleCultureDictionary dictionary		= new(culture);
		Dictionary<string, string> translations	= CsvParser.ParseString(content);

		foreach (KeyValuePair<string, string> kvp in translations)
			dictionary.AddOrUpdate(kvp.Key, kvp.Value);

		return dictionary;
	}

	#endregion Private Methods
}
