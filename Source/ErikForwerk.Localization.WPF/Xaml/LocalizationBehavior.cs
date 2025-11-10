
using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Runtime.CompilerServices;

using ErikForwerk.Localization.WPF.CoreLogic;
using ErikForwerk.Localization.WPF.Enums;
using ErikForwerk.Localization.WPF.Interfaces;

[assembly: InternalsVisibleTo("ErikForwerk.Localization.WPF.Tests")]

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Xaml;

//-----------------------------------------------------------------------------------------------------------------------------------------
/// <summary>
/// Provides attached properties for automatic language synchronization with the localization system.
/// </summary>
public static class LocalizationBehavior
{
	//-----------------------------------------------------------------------------------------------------------------
	#region Fields

	/// <summary>
	/// Stores a mapping between framework elements and their associated property changed event handlers.
	/// </summary>
	/// <remarks>
	/// This dictionary is used to track event handler registrations for property changes on specific framework elements.
	/// It enables efficient management of event subscriptions, such as adding or removing handlers as needed.
	/// </remarks>
	private static readonly Dictionary<FrameworkElement, ITranslationChanged.LocalizationChangedHandler> HANDLERS = [];

	#endregion Fields

	//-----------------------------------------------------------------------------------------------------------------
	#region SyncLanguage Dependency Property

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

	#endregion SyncLanguage Dependency Property

	//-----------------------------------------------------------------------------------------------------------------
	#region SyncDelay Dependency Property

	/// <summary>
	/// Identifies the SyncDelay attached dependency property.
	/// Values are of type int and represent the delay in milliseconds
	/// before synchronizing the <see cref="Window.Language"/> property after a culture change."/>
	/// </summary>
	/// <remarks>This field is used to register the SyncDelay attached property with the WPF property system. You
	/// typically use this identifier when calling methods such as SetValue or GetValue on elements that support the
	/// SyncDelay property.</remarks>
	public static DependencyProperty SyncDelayProperty { get; } =
		DependencyProperty.RegisterAttached(
			"SyncDelay",
			typeof(int),
			typeof(LocalizationBehavior),
			new PropertyMetadata(0));

	public static int GetSyncDelay(DependencyObject obj)
		=> (int)obj.GetValue(SyncDelayProperty);

	public static void SetSyncDelay(DependencyObject obj, int value)
		=> obj.SetValue(SyncDelayProperty, value);

	#endregion SyncDelay Dependency Property

	//-----------------------------------------------------------------------------------------------------------------
	#region Methods

	private static void OnSyncLanguageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not FrameworkElement element)
			return;

		if ((bool)e.NewValue)
		{
			//--- this is impossible, since the [OnSyncLanguageChanged]-event is not raised when the same value is set twice ---
			//--- prevent multiple registrations ---
			//if (HANDLERS.ContainsKey(element))
			//	return;

			//--- initial update ---
			UpdateElementLanguageDelayed(element);

			void fnCultureChangedHandler(ELocalizationChanges changes)
			{
				if (changes.HasFlag(ELocalizationChanges.CurrentCulture))
					UpdateElementLanguageDelayed(element);
			}

			HANDLERS[element] = fnCultureChangedHandler;
			TranslationCoreBindingSource.Instance.RegisterCallback(fnCultureChangedHandler);

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
		if(HANDLERS.TryGetValue(element, out ITranslationChanged.LocalizationChangedHandler? handler))
		{
			TranslationCoreBindingSource.Instance.UnregisterCallback(handler);
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
	private static async void UpdateElementLanguageDelayed(FrameworkElement element)
	{
		int delay = GetSyncDelay(element);

		if (delay <= 0)
			await UpdateElementLanguage(element);

		else
		{
			//--- Vorfreude ist die schönste Freude ---
			//--- therefore we await twice ---
			await await Task
				.Delay(delay, CancellationToken.None)
				.ContinueWith(_ => UpdateElementLanguage(element));
		}
	}

	internal static async Task UpdateElementLanguage(FrameworkElement element)
	{
		await element
			.Dispatcher
			.InvokeAsync(() =>
			{
				CultureInfo culture	= TranslationCoreBindingSource.Instance.CurrentCulture;
				element.Language	= XmlLanguage.GetLanguage(culture.IetfLanguageTag);
			});
	}

	/// <summary>
	/// Releases all culture change event handlers and unsubscribes from the Unloaded event for tracked framework elements.
	/// This is especially important for Unit-Tests to avoid memory leaks and ensure proper cleanup of resources.
	/// </summary>
	/// <remarks>
	/// Call this method to remove all event subscriptions and clear internal handler tracking.
	/// This is typically used during application shutdown or when disposing resources to prevent memory leaks.
	/// After calling this method, no culture change notifications will be delivered to previously tracked elements.
	/// </remarks>
	internal static void CleanUp()
	{
		foreach (FrameworkElement element in HANDLERS.Keys.ToList())
		{
			UnsubscribeCultureChangedHandler(element);
			element.Unloaded -= OnElementUnloaded;
		}

		HANDLERS.Clear();
	}

	#endregion Methods
}
