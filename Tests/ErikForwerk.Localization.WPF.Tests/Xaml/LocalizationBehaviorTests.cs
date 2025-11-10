
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;

using ErikForwerk.Localization.WPF.CoreLogic;
using ErikForwerk.Localization.WPF.Interfaces;
using ErikForwerk.Localization.WPF.Xaml;
using ErikForwerk.TestAbstractions.STA.Models;

using Xunit.Abstractions;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Tests.Xaml;

//-----------------------------------------------------------------------------------------------------------------------------------------
[Collection("82A46DF4-F8CA-4E66-8606-DF49164DEFBB")]
public class LocalizationBehaviorIntegrationTests(ITestOutputHelper toh) : StaTestBase(toh), IDisposable
{
	//-----------------------------------------------------------------------------------------------------------------
	#region Test Cleanup

	private readonly TranslationCoreBindingSource.TestModeTracker _testModetracker = new ();

	public void Dispose()
	{
		_testModetracker.Dispose();
		GC.SuppressFinalize(this);
	}

	#endregion Test Cleanup

	//-------------------------------------------------------------------------------------------------------------
	#region Test Helper

	private static int LocalizationBehavior_GetHandlerCount()
	{
		FieldInfo privateInternalDictionary = typeof(LocalizationBehavior)
			.GetField("HANDLERS", BindingFlags.NonPublic | BindingFlags.Static)!;

		Dictionary<FrameworkElement, ITranslationChanged.LocalizationChangedHandler> handlers =
			(Dictionary<FrameworkElement, ITranslationChanged.LocalizationChangedHandler>)privateInternalDictionary.GetValue(null)!;

		return handlers.Count;
	}

	#endregion Test Helper


	[STATheory]
	[InlineData("en-us")]
	[InlineData("de-de")]
	[InlineData("jp-ja")]
	public void RealWorldScenario_ComplexUITree_ShouldSynchronizeAllElements(string langName)
	{
		RunOnSTAThread(async () =>
		{
			//--- ARRANGE ---------------------------------------------------------
			Window window			= new();
			Grid grid				= new();
			StackPanel stackPanel	= new();
			TextBlock textBlock1	= new();
			TextBlock textBlock2	= new();
			Button button			= new();

			_ = stackPanel.Children.Add(textBlock1);
			_ = stackPanel.Children.Add(textBlock2);
			_ = stackPanel.Children.Add(button);
			_ = grid.Children.Add(stackPanel);
			window.Content = grid;


			LocalizationBehavior.SetSyncLanguage(textBlock1, true);
			LocalizationBehavior.SetSyncLanguage(textBlock2, true);
			LocalizationBehavior.SetSyncLanguage(button, true);

			//--- ACT -------------------------------------------------------------
			TranslationCoreBindingSource.Instance.CurrentCulture = new CultureInfo(langName);

			// wait a bit to ensure async updates have completed
			await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

			//--- ASSERT ----------------------------------------------------------
			Assert.Equal(langName, textBlock1.Language.IetfLanguageTag);
			Assert.Equal(langName, textBlock2.Language.IetfLanguageTag);
			Assert.Equal(langName, button.Language.IetfLanguageTag);

			TestConsole.WriteLine("[✔️ PASSED] All elements synchronized correctly.");
		}
		, () => LocalizationBehavior.CleanUp());
	}

	[STATheory]
	[InlineData("en-us")]
	[InlineData("de-de")]
	[InlineData("jp-ja")]
	public void RealWorldScenario_ComplexUITreeWithDelay_ShouldSynchronizeAfterDelay(string langName)
	{
		RunOnSTAThread(async () =>
		{
			//--- ARRANGE ---------------------------------------------------------
			Window window			= new();
			Grid grid				= new();
			StackPanel stackPanel	= new();
			TextBlock textBlock1	= new();
			TextBlock textBlock2	= new();
			Button button			= new();

			_ = stackPanel.Children.Add(textBlock1);
			_ = stackPanel.Children.Add(textBlock2);
			_ = stackPanel.Children.Add(button);
			_ = grid.Children.Add(stackPanel);
			window.Content = grid;

			string initialLanguage	= CultureInfo.InvariantCulture.IetfLanguageTag;
			textBlock1.Language		= XmlLanguage.GetLanguage(initialLanguage);
			textBlock2.Language		= XmlLanguage.GetLanguage(initialLanguage);
			button.Language			= XmlLanguage.GetLanguage(initialLanguage);

			LocalizationBehavior.SetSyncLanguage(textBlock1, true);
			LocalizationBehavior.SetSyncLanguage(textBlock2, true);
			LocalizationBehavior.SetSyncLanguage(button, true);
			LocalizationBehavior.SetSyncDelay(textBlock1, 100);
			LocalizationBehavior.SetSyncDelay(textBlock2, 100);
			LocalizationBehavior.SetSyncDelay(button, 100);

			//--- ACT -------------------------------------------------------------
			TranslationCoreBindingSource.Instance.CurrentCulture = new CultureInfo(langName);

			// wait a bit to ensure dispatcher has processed
			await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

			//--- ASSERT (immediately) --------------------------------------------
			Assert.Equal(initialLanguage, textBlock1.Language.IetfLanguageTag);
			Assert.Equal(initialLanguage, textBlock2.Language.IetfLanguageTag);
			Assert.Equal(initialLanguage, button.Language.IetfLanguageTag);

			TestConsole.WriteLine("[✔️ PASSED] Elements NOT synchronized immediately (delay active).");

			// wait for delay to complete (100ms delay + 10ms buffer)
			await Task.Delay(110);
			await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

			//--- ASSERT (after delay) --------------------------------------------
			Assert.Equal(langName, textBlock1.Language.IetfLanguageTag);
			Assert.Equal(langName, textBlock2.Language.IetfLanguageTag);
			Assert.Equal(langName, button.Language.IetfLanguageTag);

			TestConsole.WriteLine("[✔️ PASSED] All elements synchronized correctly after delay.");
		}
		, () => LocalizationBehavior.CleanUp());
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetSyncLanguage_ReturnsSetValue(bool expectedValue)
	{
		RunOnSTAThread(() =>
		{
			//--- ARRANGE ---------------------------------------------------------
			TextBlock element = new();
			LocalizationBehavior.SetSyncLanguage(element, expectedValue);

			//--- ACT -------------------------------------------------------------
			bool actualValue = LocalizationBehavior.GetSyncLanguage(element);

			//--- ASSERT ----------------------------------------------------------
			Assert.Equal(expectedValue, actualValue);

			TestConsole.WriteLine($"[✔️ PASSED] GetSyncLanguage returned expected value: [{expectedValue}]");
		}
		, () => LocalizationBehavior.CleanUp());
	}

	[Theory]
	[InlineData(0)]
	[InlineData(100)]
	[InlineData(500)]
	public void GetSyncDelay_ReturnsSetValue(int expectedValue)
	{
		RunOnSTAThread(() =>
		{
			//--- ARRANGE ---------------------------------------------------------
			TextBlock element = new();
			LocalizationBehavior.SetSyncDelay(element, expectedValue);
			//--- ACT -------------------------------------------------------------
			int actualValue = LocalizationBehavior.GetSyncDelay(element);
			//--- ASSERT ----------------------------------------------------------
			Assert.Equal(expectedValue, actualValue);
			TestConsole.WriteLine($"[✔️ PASSED] GetSyncDelay returned expected value: [{expectedValue}]");
		}
		, () => LocalizationBehavior.CleanUp());
	}

	[STAFact]
	public void MemoryLeak_MultipleLoadUnload_ShouldNotLeakMemory()
	{
		RunOnSTAThread(async () =>
		{
			//--- ARRANGE ---------------------------------------------------------
			List<WeakReference> elements = [];

			//--- ACT -------------------------------------------------------------
			for (int i = 0; i < 100; i++)
			{
				TextBlock element = new();
				LocalizationBehavior.SetSyncLanguage(element, true);
				elements.Add(new WeakReference(element));
				element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
			}

			TestConsole.WriteLine($"Created elements:   [{elements.Count}]");

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			TestConsole.WriteLine("Garbage Collection executed.");

			// wait a bit to ensure async updates have completed
			await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

			//--- ASSERT ----------------------------------------------------------
			int aliveCount = elements.Count(wr => wr.IsAlive);
			TestConsole.WriteLine($"Alive elements:     [{elements.Count(wr => wr.IsAlive)}]");
			Assert.True(aliveCount < 50, $"Too many objects still in memory: {aliveCount}");
		}
		, () => LocalizationBehavior.CleanUp());
	}

	// New test: ensure the else-branch in OnSyncLanguageChanged unsubscribes and prevents further updates
	/// <summary>
	/// Tests that deactivating language synchronization prevents further updates to the
	/// System.Windows.FrameworkElement.Language property when the culture changes.
	/// (The else-branch in <see cref="LocalizationBehavior.OnSyncLanguageChanged"/> is executed.)
	/// </summary>
	/// <remarks>
	/// This test verifies that when LocalizationBehavior.SetSyncLanguage is set to  false, the element's
	/// language remains unchanged even if the current culture  in TranslationCoreBindingSource.Instance is updated.
	/// This ensures that the synchronization mechanism is properly unsubscribed.
	/// </remarks>
	[STAFact]
	public void OnSyncLanguageChanged_Deactivation_ShouldPreventFurtherLanguageUpdates()
	{
		RunOnSTAThread(() =>
		{
			//--- ARRANGE ---------------------------------------------------------
			TextBlock element = new();

			// Activate synchronization and perform an initial culture change
			LocalizationBehavior.SetSyncLanguage(element, true);
			TranslationCoreBindingSource.Instance.CurrentCulture = new CultureInfo("en-us");
			Assert.Equal("en-us", element.Language.IetfLanguageTag);

			//--- ACT: deactivate synchronization (this should trigger the else-branch) ---
			LocalizationBehavior.SetSyncLanguage(element, false);

			// Change culture again; element.Language should NOT be updated
			TranslationCoreBindingSource.Instance.CurrentCulture = new CultureInfo("de-de");

			//--- ASSERT ----------------------------------------------------------
			Assert.Equal("en-us", element.Language.IetfLanguageTag);
		}
		, () => LocalizationBehavior.CleanUp());
	}

	[STAFact]
	public void OnSyncLanguageChanged_NonFrameworkElement_ShouldDoNothing()
	{
		//--- ARRANGE ---------------------------------------------------------
		RunOnSTAThread(() =>
		{
			DependencyObject obj = new();
			int originalHandlerElements			= LocalizationBehavior_GetHandlerCount();

			//--- ACT ---------------------------------------------------------
			// should not throw when activating or deactivating
			LocalizationBehavior.SetSyncLanguage(obj, true);
			int shouldBeUnchangedCount			= LocalizationBehavior_GetHandlerCount();

			LocalizationBehavior.SetSyncLanguage(obj, false);
			int shouldStillBeUnchangedCount		= LocalizationBehavior_GetHandlerCount();

			//--- ASSERT ------------------------------------------------------
			Assert.Equal(originalHandlerElements, shouldBeUnchangedCount);
			Assert.Equal(originalHandlerElements, shouldStillBeUnchangedCount);
		}
		, () => LocalizationBehavior.CleanUp());
	}

	[STAFact]
	public void OnSyncLanguageChanged_SameElementRepeatedly_ShouldNotRegisterMultipleHandlers()
	{
		//--- ARRANGE ---------------------------------------------------------
		RunOnSTAThread(() =>
		{
			TextBlock element = new();
			int originalHandlerElements			= LocalizationBehavior_GetHandlerCount();

			//--- ACT ---------------------------------------------------------
			LocalizationBehavior.SetSyncLanguage(element, true);
			LocalizationBehavior.SetSyncLanguage(element, true); // repeat activation
			int afterRepeatActivationCount		= LocalizationBehavior_GetHandlerCount();

			LocalizationBehavior.SetSyncLanguage(element, false);
			LocalizationBehavior.SetSyncLanguage(element, false); // repeat deactivation
			int afterRepeatDeactivationCount	= LocalizationBehavior_GetHandlerCount();

			TestConsole.WriteLine($"Original handler count:          [{originalHandlerElements}]");
			TestConsole.WriteLine($"After repeat activation count:   [{afterRepeatActivationCount}]");
			TestConsole.WriteLine($"After repeat deactivation count: [{afterRepeatDeactivationCount}]");

			//--- ASSERT ------------------------------------------------------
			Assert.Equal(originalHandlerElements + 1, afterRepeatActivationCount);
			Assert.Equal(originalHandlerElements, afterRepeatDeactivationCount);
		}
		, () => LocalizationBehavior.CleanUp());
	}

	[STAFact]
	public async Task UpdateElementLanguageAsync_SetsLanguageToCurrentCulture()
	{
		RunOnSTAThread(async () =>
		{
			//--- ARRANGE -----------------------------------------------------
			FrameworkElement element	= new();
			CultureInfo expectedCulture	= new("fr-FR");
			TranslationCoreBindingSource.Instance.CurrentCulture = expectedCulture;

			//--- if this is set from the beginning, this test would be pointless ---
			Assert.NotEqual(expectedCulture.IetfLanguageTag, element.Language.IetfLanguageTag);

			//--- ACT ---------------------------------------------------------
			await LocalizationBehavior.UpdateElementLanguageAsync(element);

			//--- ASSERT ------------------------------------------------------
			Assert.Equal(expectedCulture.IetfLanguageTag, element.Language.IetfLanguageTag, true);
		}
		, () => LocalizationBehavior.CleanUp());
	}

	[STAFact]
	public void UpdateElementLanguage_SetsLanguageToCurrentCulture()
	{
		RunOnSTAThread(() =>
		{
			//--- ARRANGE -----------------------------------------------------
			FrameworkElement element	= new();
			CultureInfo expectedCulture	= new("fr-FR");
			TranslationCoreBindingSource.Instance.CurrentCulture = expectedCulture;

			//--- if this is set from the beginning, this test would be pointless ---
			Assert.NotEqual(expectedCulture.IetfLanguageTag, element.Language.IetfLanguageTag);

			//--- ACT ---------------------------------------------------------
			LocalizationBehavior.UpdateElementLanguage(element);

			//--- ASSERT ------------------------------------------------------
			Assert.Equal(expectedCulture.IetfLanguageTag, element.Language.IetfLanguageTag, true);
		}
		, () => LocalizationBehavior.CleanUp());
	}
}