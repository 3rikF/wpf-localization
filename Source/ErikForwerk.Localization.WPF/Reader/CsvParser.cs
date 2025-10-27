using System.IO;
using System.Text;

namespace ErikForwerk.Localization.WPF.Reader;

/// <summary>
/// Parser for CSV files in the format "key,translation" with optional comments.
/// Supports parsing CSV data from streams and converting it into <see cref="Dictionary{string, string}"/> objects.
/// </summary>
internal sealed class CsvParser
{
	//-----------------------------------------------------------------------------------------------------------------
	#region Fields

	private const char CSV_SEPARATOR		= ';';
	private const string COMMENT_PREFIX		= "//";

	#endregion Fields

	//-----------------------------------------------------------------------------------------------------------------
	#region Public Methods

	/// <summary>
	/// Parses a string with CSV data and populates a <see cref="Dictionary{string, string}"/>.
	/// </summary>
	/// <param name="csvContent">The CSV content as a string</param>
	/// <returns>A dictionary with the parsed translations</returns>
	public static Dictionary<string, string> ParseString(ReadOnlySpan<char> csvContent)
	{
		using MemoryStream stream = new(Encoding.UTF8.GetBytes(csvContent.ToArray()));
		return ParseStream(stream);
	}

	/// <summary>
	/// Parses a stream with CSV data in the format "key,translation" and populates a <see cref="Dictionary{string, string}"/>.
	/// </summary>
	/// <param name="stream">The stream with CSV data</param>
	/// <returns>A dictionary with the parsed translations</returns>
	public static Dictionary<string, string> ParseStream(Stream stream)
	{
		ArgumentNullException.ThrowIfNull(stream);

		Dictionary<string, string> dictionary = [];

		using StreamReader reader = new(stream, Encoding.UTF8, leaveOpen: true);
		string? line;

		while ((line = reader.ReadLine()) != null)
		{
			line = line.Trim();

			if (string.IsNullOrWhiteSpace(line))
				continue;

			if (line.StartsWith(COMMENT_PREFIX))
				continue;

			(string? key, string? value) = ParseLine(line);

			if (!string.IsNullOrEmpty(key))
				dictionary.Add(key.Trim(), UnescapeLocalization(value));
		}

		return dictionary;
	}

	#endregion Public Methods

	//-----------------------------------------------------------------------------------------------------------------
	#region Private Methods

	private static (string key, string value) ParseLine(ReadOnlySpan<char> line)
	{
		string[] parts = SplitLine(line);

		if (parts.Length < 2)
			return (string.Empty, string.Empty);

		return (parts[0], parts[1]);
	}

	private static string UnescapeLocalization(string field)
	{
		return field
			.Trim()
			.Replace("\\r\\n", "\r\n")
			.Replace("\\r", "\r")
			.Replace("\\n", "\n")
			.Replace("\\t", "\t")
			;
	}

	private static string[] SplitLine(ReadOnlySpan<char> line)
	{
		List<string> finalParts = [];
		MemoryExtensions.SpanSplitEnumerator<char> tmpParts = line.Split(CSV_SEPARATOR);

		//--- if a span ends with a '\' and there is a next span, join those two spans back together ---
		foreach (Range range in tmpParts)
		{
			ReadOnlySpan<char> item = line[range];

			//--- remove the trailing '\' from the last item and join with the current item ---
			if (finalParts.Count > 0 && finalParts[^1].EndsWith('\\') && !item.IsEmpty)
				finalParts[^1] = finalParts[^1][..^1] + CSV_SEPARATOR + item.ToString();

			else
				finalParts.Add(item.ToString());
		}

		//---------------------------------------------------------------------
		return [.. finalParts];
	}

	#endregion Private Methods
}
