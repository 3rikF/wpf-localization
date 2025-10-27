
using System.Globalization;
using System.IO;

using ErikForwerk.Localization.WPF.Interfaces;
using ErikForwerk.Localization.WPF.Models;
using ErikForwerk.Localization.WPF.Tools;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Reader;

//-----------------------------------------------------------------------------------------------------------------------------------------
public sealed class FilesystemCsvReader : ILocalizationReader
{
	//-----------------------------------------------------------------------------------------------------------------
	#region Fields

	private readonly string _languagesFolder;

	#endregion Fields

	//-----------------------------------------------------------------------------------------------------------------
	#region Constructor

	public FilesystemCsvReader(string languagesFolder)
	{
		_languagesFolder = languagesFolder ?? throw new ArgumentNullException(nameof(languagesFolder));

		// Ensure directory exists
		if (!Directory.Exists(_languagesFolder))
			throw new DirectoryNotFoundException($"Languages directory not found: {_languagesFolder}");
	}

	#endregion Constructor

	//-----------------------------------------------------------------------------------------------------------------
	#region ILocalizationReader

	public ISingleCultureDictionary[] GetLocalizations()
	{
		//--- Get all CSV files in the languages folder ---
		string[] languageFiles = Directory.GetFiles(_languagesFolder, "*.csv");

		List<SingleCultureDictionary> dictionaries = [];

		foreach (string file in languageFiles)
		{
			if (!Helper.TryGetCultureFromResourceName(file, out CultureInfo? csvCulture))
				throw new InvalidOperationException($"The resource name must contain a IETF-language-tag (like de-DE).");

			SingleCultureDictionary dictionary = LoadLanguageFile(file, csvCulture);
			dictionaries.Add(dictionary);
		}

		return [.. dictionaries];
	}

	#endregion ILocalizationReader

	//-----------------------------------------------------------------------------------------------------------------
	#region Private Methods

	private SingleCultureDictionary LoadLanguageFile(string filePath, CultureInfo culture)
	{
		SingleCultureDictionary dictionary = new(culture);

		using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		Dictionary<string, string> translations = CsvParser.ParseStream(stream);

		foreach (KeyValuePair<string, string> kvp in translations)
		{
			dictionary.AddOrUpdate(kvp.Key, kvp.Value);
		}

		return dictionary;
	}

	#endregion Private Methods
}
