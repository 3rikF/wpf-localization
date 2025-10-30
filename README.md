# WPF Localization Framework

A lightweight and easy-to-use localization framework for WPF/XAML applications that enables runtime language switching with minimal configuration.

## Features

This framework aims to simplify internationalization (i18n) in WPF applications by providing:

- **Simple XAML Integration**: Use the `loc:Localization` markup extension directly in XAML
- **Runtime Language Switching**: Change languages on the fly without restarting your application
- **CSV-based Translations**: Easy-to-manage translation files in CSV format
- **Dynamic Binding Support**: Bind translation keys dynamically using data binding
- **Placeholder Support**: Include dynamic content in translations using placeholders
- **Resource Embedding**: Embed translation files as resources in your assembly
- **Support for any CultureInfo**: Works with any culture supported by .NET
- **Automatic Culture Detection**: Detects and uses appropriate cultures automatically
- **Missing Translation Detection**: Clearly marks missing translations for easy identification

## Installation

### NuGet Package

```bash
dotnet add package ErikForwerk.Localization.WPF
```

Or via NuGet Package Manager:

```
Install-Package ErikForwerk.Localization.WPF
```

## Usage

### 1. Create Translation Files

Create CSV files for each language you want to support. The format is simple: `Key;Translation`

The resource name/filename must contain a culture name (e.g. "en-US", "de-DE", etc). The preferred format is `filename.de-DE.csv`.

**Languages/en-US.csv:**
```csv
// English language translations (en-US)
OtherTextKey;Other Text
BindingLanguageKey;Binding Language Key
Button:ChangeText;Change Text
PartA;Part A
PartB;Part B
```

### 2. Initialize LocalizationController

In your ViewModel or code-behind, initialize the `LocalizationController` and load your translation files:

```csharp
using System.Globalization;
using ErikForwerk.Localization.WPF.Models;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private LocalizationController _localization = new(System.Windows.Application.Current.MainWindow);

    public MainWindowViewModel()
    {
        // Load translations from embedded resources
        _localization.AddTranslationsFromCsvResource("Languages/de-DE.csv");
        _localization.AddTranslationsFromCsvResource("Languages/en-US.csv");
        _localization.AddTranslationsFromCsvResource("Languages/ru-RU.csv");
        _localization.AddTranslationsFromCsvResource("Languages/ja-JP.csv");
    }

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
}
```

### 3. Use in XAML

Add the XML namespace to your XAML file:

```xaml
xmlns:loc="clr-namespace:ErikForwerk.Localization.WPF;assembly=ErikForwerk.Localization.WPF"
```

The language of the window will be set when initializing the `LocalizationController` instance and when changing the `CurrentCulture``on that instance using the `Window` reference, that was handed in the constructor.

#### Static Translation

Use a static translation key directly:

```xaml
<TextBlock Text="{loc:Localization StaticTextKey}" />
```

#### Dynamic Translation with Binding

Bind the translation key dynamically:

```xaml
<TextBlock Text="{loc:Localization {Binding DynamicLangKey}}" />
```

In your ViewModel:

```csharp
private string _dynamicLangKey = "BindingLanguageKey";

public string DynamicLangKey
{
    get => _dynamicLangKey;
    set
    {
        if (value == _dynamicLangKey)
            return;
        
        _dynamicLangKey = value;
        RaisePropertyChanged();
    }
}
```

#### Translation with Placeholders

Use the placeholders synthax in your translations to replacedifferent parts at runtime:

```xaml
<TextBlock Text="{loc:Localization 'Foobar: %PartA% + %PartB%', ParsePlaceholders=True}" />
```

You can have non-translated text in combination with the placeholders, but only language-keys within %..% will be replaced.
In the above example, the placeholders (`PartA`, `PartB`) will be replaced with their corresponding translations from the CSV file.
You can also use binding in combination with placeholders.\
To use % as part of the language key withing the placeholder-syntax, you need the escape the `%` using `\%` in the placeholder, for example  `%Value\%Offset%`.


#### Language Selector

Create a ComboBox to let users switch languages:

```xaml
<ComboBox
    DisplayMemberPath	="DisplayName"
    ItemsSource			="{Binding SupportedCultures}"
    SelectedItem		="{Binding SelectedCulture}" />
```

### 4. Handling Missing Translations

If a translation key is not found, the framework will display the key surrounded by exclamation marks, making it easy to identify missing translations:

```xaml
<TextBlock Text="{loc:Localization MissingLangKey}" />
<!-- Will display "!!!MissingLangKey!!!" if the key doesn't exist -->
```

## Example Project

For a complete working example, check out the [WpfLocalizationExample](./Examples/WpfLocalizationExample) project included in this repository. It demonstrates:

- Setting up the LocalizationController
- Static and dynamic translations
- Placeholder replacement
- Runtime language switching with a ComboBox
- Multiple language support (English, German, Russian, Japanese)

## Requirements

- .NET 9.0 or higher
- WPF Application

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
