
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

using ErikForwerk.Localization.WPF.CoreLogic;
using ErikForwerk.Localization.WPF.Models;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Xaml;

//-----------------------------------------------------------------------------------------------------------------------------------------
public sealed class LocalizationExtension : MarkupExtension
{
	//-----------------------------------------------------------------------------------------------------------------
	#region Construction

	public LocalizationExtension()
	{ }

	/// <summary>
	/// Initializes a new instance of the LocalizationExtension class
	/// that directly uses the specified localization key.
	/// </summary>
	/// <param name="key">The localization key that identifies the resource to be used for localization.</param>
	public LocalizationExtension(string key)
	{
		Key = key;
	}

	/// <summary>
	/// Initializes a new instance of the LocalizationExtension class using the specified binding
	/// to provide a dynamic localization key.
	/// </summary>
	/// <param name="keyBinding">
	/// An object representing the key or identifier used for localization binding.
	/// This value determines which localized resource will be retrieved or bound.
	/// </param>
	public LocalizationExtension(object keyBinding)
	{
		KeyBinding = keyBinding;
	}

	#endregion Construction

	//-----------------------------------------------------------------------------------------------------------------
	#region Properties

	/// <summary>
	/// Gets or sets the unique key associated with this instance.
	/// </summary>
	public string Key { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the key binding associated with the control.
	/// </summary>
	/// <remarks>
	/// The value can be used to define or retrieve the input binding for keyboard interactions.
	/// The type and expected structure of the binding object may depend on the control's implementation
	/// and the data binding framework in use.
	/// </remarks>
	public object? KeyBinding { get; set; } = null;

	/// <summary>
	/// Gets or sets a value indicating whether placeholders (e.g., %Key%) in the translated text should be parsed and replaced.
	/// </summary>
	/// <remarks>
	/// When set to true, the localization system will search for placeholders in the format %Key% and replace them
	/// with their corresponding translations. This feature should only be enabled when placeholders are actually used
	/// to avoid unnecessary performance overhead.
	/// Default value is false for optimal performance.
	/// </remarks>
	public bool ParsePlaceholders { get; set; } = false;

	#endregion Properties

	//-----------------------------------------------------------------------------------------------------------------
	#region Methods

	/// <summary>
	/// Provides a value for XAML binding that resolves the localized text for the specified key or binding expression.
	/// </summary>
	/// <remarks>
	/// This method is called by the XAML infrastructure when loading markup and supports both static keys
	/// and dynamic binding scenarios. If a binding expression is provided, a MultiBinding is used to combine the key and
	/// the translation source, enabling dynamic localization updates. For static keys, a single binding with a converter
	/// is used. The returned value is suitable for use in XAML properties that expect localized text and will update
	/// automatically if the underlying localization data changes.
	/// </remarks>
	/// <param name="serviceProvider">
	/// An object that provides services for the markup extension.
	/// Typically supplied by the XAML parser during value resolution.
	/// </param>
	/// <returns>
	/// An object that supplies the localized text for the target property in XAML.
	/// The returned value is either a binding or multibinding configured to retrieve and convert the localized string.
	/// </returns>
	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		//--- multibinding case -----------------------------------------------
		if (KeyBinding is BindingExpression or Binding)
		{
			Binding keyBinding = KeyBinding as Binding ?? new Binding();

			// Binding zum TranslationSource erstellen
			Binding translationBinding = new()
			{
				Source	= TranslationCoreBindingSource.Instance,										// the translation-source singleton
				Path	= new PropertyPath(nameof(TranslationCoreBindingSource.LocalizedText)),
				Mode	= BindingMode.OneWay
			};

			// MultiBinding erstellen
			MultiBinding multiBinding = new ()
			{
				Bindings				= { keyBinding, translationBinding}
				, Mode					= BindingMode.OneWay
				//--- des Pudels Kern ---
				, Converter				= new LocalizationDynamicTextConverter()
				, ConverterParameter	= new PlaceholderConverterParameter(keyBinding.Path?.Path, ParsePlaceholders)
			};

			return multiBinding.ProvideValue(serviceProvider);
		}

		//--- single binding case ---------------------------------------------
		else
		{
			Binding binding = new()
			{
				Source		= TranslationCoreBindingSource.Instance,									// the translation-source singleton
				Path		= new PropertyPath(nameof(TranslationCoreBindingSource.LocalizedText)),
				Mode		= BindingMode.OneWay,
				//--- des Pudels Kern ---
				Converter	= new LocalizationTextConverter(Key, ParsePlaceholders)
			};

			return binding.ProvideValue(serviceProvider);
		}
	}

	#endregion Methods
}
