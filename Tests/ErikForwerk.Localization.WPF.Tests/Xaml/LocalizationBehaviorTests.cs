
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

using ErikForwerk.Localization.WPF.CoreLogic;
using ErikForwerk.Localization.WPF.Xaml;
using ErikForwerk.TestAbstractions.STA.Models;

using Xunit.Abstractions;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Tests.Xaml;

//-----------------------------------------------------------------------------------------------------------------------------------------
[Collection("STA")]
public class LocalizationBehaviorIntegrationTests(ITestOutputHelper toh) : StaTestBase(toh)
{
	[STATheory]
	[InlineData("en-us")]
	[InlineData("de-de")]
	[InlineData("jp-ja")]
	public void RealWorldScenario_ComplexUITree_ShouldSynchronizeAllElements(string langName)
	{
		RunOnSTAThread(() =>
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

			//--- ASSERT ----------------------------------------------------------
			Assert.Equal(langName, textBlock1.Language.IetfLanguageTag);
			Assert.Equal(langName, textBlock2.Language.IetfLanguageTag);
			Assert.Equal(langName, button.Language.IetfLanguageTag);

			TestConsole.WriteLine("[✔️ PASSED] All elements synchronized correctly.");
		}
		, () => LocalizationBehavior.CleanUp());
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ZZGetSyncLanguage_ReturnsSetValue(bool expectedValue)
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

	[STAFact]
	public void MemoryLeak_MultipleLoadUnload_ShouldNotLeakMemory()
	{
		TranslationCoreBindingSource.ResetInstance();
		RunOnSTAThread(() =>
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


			//--- ASSERT ----------------------------------------------------------
			int aliveCount = elements.Count(wr => wr.IsAlive);
			TestConsole.WriteLine($"Alive elements:     [{elements.Count(wr => wr.IsAlive)}]");
			Assert.True(aliveCount < 50, $"Zu viele Objekte noch im Speicher: {aliveCount}");
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
		TranslationCoreBindingSource.ResetInstance();
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

	[Fact]
	public void  OnSyncLanguageChanged
}