using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;

using ErikForwerk.Localization.WPF.CoreLogic;

namespace ErikForwerk.Localization.WPF.Xaml;

/// <summary>
/// Provides attached properties for automatic language synchronization with the localization system.
/// </summary>
public static class LocalizationBehavior
{
	/// <summary>
	/// Stores a mapping between framework elements and their associated property changed event handlers.
	/// </summary>
	/// <remarks>
	/// This dictionary is used to track event handler registrations for property changes on specific framework elements.
	/// It enables efficient management of event subscriptions, such as adding or removing handlers as needed.
	/// </remarks>
	private static readonly Dictionary<FrameworkElement, PropertyChangedEventHandler> HANDLERS = [];

	/// <summary>
	/// Attached property that enables automatic synchronization of the element's Language property
	/// with the current localization culture.
	/// </summary>
	public static DependencyProperty SyncLanguageProperty { get; } =
		DependencyProperty.RegisterAttached(
			"SyncLanguage",
			typeof(bool),
			typeof(LocalizationBehavior),
			new PropertyMetadata(false, OnSyncLanguageChanged));

	public static bool GetSyncLanguage(DependencyObject obj)
		=> (bool)obj.GetValue(SyncLanguageProperty);

	public static void SetSyncLanguage(DependencyObject obj, bool value)
		=> obj.SetValue(SyncLanguageProperty, value);

	private static void OnSyncLanguageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not FrameworkElement element)
			return;

		if ((bool)e.NewValue)
		{
			//--- prevent multiple registrations ---
			if (HANDLERS.ContainsKey(element))
				return;

			//--- initial update ---
			UpdateElementLanguage(element);

			void fnCultureChangedHandler(object? sender, PropertyChangedEventArgs args)
			{
				if (args.PropertyName == nameof(TranslationCoreBindingSource.CurrentCulture))
					UpdateElementLanguage(element);
			}

			HANDLERS[element] = fnCultureChangedHandler;
			TranslationCoreBindingSource.Instance.PropertyChanged += fnCultureChangedHandler;

			//--- clean-up on unload ---
			element.Unloaded += OnElementUnloaded;
		}
		else
		{
			//--- clean-up on deactivation ---
			UnsubscribeCultureChangedHandler(element);
		}
	}

	/// <summary>
	/// Handles the Unloaded event for a FrameworkElement by removing event handlers
	/// and unsubscribing from culture change notifications.
	/// </summary>
	/// <remarks>
	/// This method ensures that event handlers and culture change subscriptions are properly cleaned up
	/// when a FrameworkElement is unloaded, helping to prevent memory leaks and unintended behavior.
	/// </remarks>
	/// <param name="sender">The source of the event, expected to be a FrameworkElement whose Unloaded event is being handled.</param>
	/// <param name="e">The event data associated with the Unloaded event.</param>
	private static void OnElementUnloaded(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement element)
		{
			UnsubscribeCultureChangedHandler(element);
			element.Unloaded -= OnElementUnloaded;
		}
	}

	/// <summary>
	/// Unsubscribes the culture changed event handler associated with the specified element,
	/// preventing further notifications of culture changes for that element.
	/// </summary>
	/// <remarks>
	/// Call this method when the specified element no longer needs to respond to culture changes,
	/// such as during cleanup or disposal. If no handler is associated with the element, the method has no effect.
	/// </remarks>
	/// <param name="element">The framework element whose culture changed event handler should be unsubscribed. Must not be null.</param>
	private static void UnsubscribeCultureChangedHandler(FrameworkElement element)
	{
		if (HANDLERS.TryGetValue(element, out PropertyChangedEventHandler? handler))
		{
			TranslationCoreBindingSource.Instance.PropertyChanged -= handler;
			_ = HANDLERS.Remove(element);
		}
	}

	/// <summary>
	/// Sets the language of the specified FrameworkElement to match the application's current culture.
	/// </summary>
	/// <remarks>
	/// This method updates the Language property using the IETF language tag of the application's current culture.
	/// This affects how text is rendered and localized within the element and its children.
	/// Ensure that the element is not null before calling this method.
	/// </remarks>
	/// <param name="element">The FrameworkElement whose Language property will be updated to reflect the current culture. Cannot be null.</param>
	private static void UpdateElementLanguage(FrameworkElement element)
	{
		CultureInfo culture = TranslationCoreBindingSource.Instance.CurrentCulture;
		element.Language = XmlLanguage.GetLanguage(culture.IetfLanguageTag);
	}
}