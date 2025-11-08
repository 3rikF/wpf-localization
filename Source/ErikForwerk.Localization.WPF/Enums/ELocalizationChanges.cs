//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Enums;

//-----------------------------------------------------------------------------------------------------------------------------------------
[Flags]
public enum ELocalizationChanges
{
	None				= 0,
	Translations		= 1,
	SupportedCultures	= 2,
	CurrentCulture		= 4,

	All					= Translations | SupportedCultures |CurrentCulture,
}
