//-----------------------------------------------------------------------------------------------------------------------------------------
using ErikForwerk.Localization.WPF.Enums;

namespace ErikForwerk.Localization.WPF.Interfaces;

//-----------------------------------------------------------------------------------------------------------------------------------------
public interface ITranslationChanged
{
	public delegate void LocalizationChangedHandler(ELocalizationChanges changes);

	void RegisterCallback(LocalizationChangedHandler callback);

	void UnregisterCallback(LocalizationChangedHandler callback);
}
