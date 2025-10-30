using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WpfLocalizationExample.ViewModels;

public sealed class ExampleTabItemViewModel : INotifyPropertyChanged
{
	public ExampleTabItemViewModel(string header, string content)
	{
		HeaderLangKey = header;
		ContentLangKey = content;
	}

	//-----------------------------------------------------------------------------------------------------------------
	#region Properties

	public string HeaderLangKey
		{ get; init; } = string.Empty;

	public string ContentLangKey
		{ get; init; } = string.Empty;

	#endregion Properties

	//-----------------------------------------------------------------------------------------------------------------
	#region Methods

	public override string ToString()
		=> $"TabItem: Header='{HeaderLangKey}', Content='{ContentLangKey}'";

	#endregion Methods

	//-----------------------------------------------------------------------------------------------------------------
	#region INotifyPropertyChanged

	public event PropertyChangedEventHandler? PropertyChanged;
	public void RaisePropertyChanged([CallerMemberName] string propertyName = "")
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	#endregion INotifyPropertyChanged
}
