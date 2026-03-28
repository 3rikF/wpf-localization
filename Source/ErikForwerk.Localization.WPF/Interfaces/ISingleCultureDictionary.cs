
using System.Globalization;

namespace ErikForwerk.Localization.WPF.Interfaces;

public interface ISingleCultureDictionary : IEnumerable<KeyValuePair<string, string>>
{
	CultureInfo Culture { get; }

	public void Add(string key, string translation);

	public void AddOrUpdate(string key, string translation);

	public void AddOrUpdate(ISingleCultureDictionary otherDict);

	public bool ContainsKey(string key);

	public string GetTranslation(string key);

	IReadOnlyDictionary<string, string> GetAllTranslations();
}
