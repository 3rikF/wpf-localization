using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using ErikForwerk.Localization.WPF.Models;
using ErikForwerk.Localization.WPF.Tools;

namespace WpfLocalizationTest.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
	//-----------------------------------------------------------------------------------------------------------------
	#region Fields

	private LocalizationController _localization = new(System.Windows.Application.Current.MainWindow);

	#endregion Fields

	//-----------------------------------------------------------------------------------------------------------------
	#region Construction

	public MainWindowViewModel()
	{
		_localization.AddTranslationsFromCsvResource("Languages/de-DE.csv");
		_localization.AddTranslationsFromCsvResource("Languages/en-US.csv");
		_localization.AddTranslationsFromCsvResource("Languages/ru-RU.csv");
		_localization.AddTranslationsFromCsvResource("Languages/ja-JP.csv");
	}

	#endregion Construction

	//-----------------------------------------------------------------------------------------------------------------
	#region Commands

	private ICommand? _cmdChangeBoundLangKey = null;
	private string _sampleLangKey = "BindingLanguageKey";

	public ICommand CmdChangeBoundLangKey
		=> _cmdChangeBoundLangKey ??= new RelayCommand(
			a => SampleLangKey = SampleLangKey == "BindingLanguageKey"
				? "AnotherBindingLanguageKey"
				: "BindingLanguageKey"
			, c => true);

	#endregion Commands

	//-----------------------------------------------------------------------------------------------------------------
	#region Properties

	public IEnumerable<CultureInfo> SupportedCultures
		=> _localization.SupportedCultures;

	public CultureInfo SelectedCulture
	{
		get => _localization.CurrentCulture;
		set
		{
			if (value == _localization.CurrentCulture)
				return;

			_localization.CurrentCulture = value;
			RaisePropertyChanged();
		}
	}

	public string SampleLangKey
	{
		get => _sampleLangKey;
		set
		{
			if (value == _sampleLangKey)
				return;

			_sampleLangKey = value;
			RaisePropertyChanged();
		}
	}
	#endregion Properties

	//-----------------------------------------------------------------------------------------------------------------
	#region INotifyPropertyChanged

	public event PropertyChangedEventHandler? PropertyChanged;

	public void RaisePropertyChanged([CallerMemberName] string propertyName = "")
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	#endregion INotifyPropertyChanged
}
