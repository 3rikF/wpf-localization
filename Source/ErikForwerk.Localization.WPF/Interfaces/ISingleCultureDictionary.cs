
using System.Globalization;

namespace ErikForwerk.Localization.WPF.Interfaces;

public interface ISingleCultureDictionary //: IDictionary<string, string>
{
	CultureInfo Culture { get; }

	public void AddOrUpdate(string key, string translation);

	public void AddOrUpdate(ISingleCultureDictionary otherDict);

	//public bool TryGetTranslation(string key, out string translation);
	public bool ContainsKey(string key);

	public string GetTranslation(string key);

	IReadOnlyDictionary<string, string> GetAllTranslations();
}
