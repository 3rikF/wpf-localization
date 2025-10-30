# WPF Localization Framework

A lightweight and easy-to-use localization framework for WPF/XAML applications that enables runtime language switching with minimal configuration.

## Goal & Intention

This framework aims to simplify internationalization (i18n) in WPF applications by providing:

- **Simple XAML Integration**: Use the `loc:Localization` markup extension directly in XAML
- **Runtime Language Switching**: Change languages on the fly without restarting your application
- **CSV-based Translations**: Easy-to-manage translation files in CSV format
- **Dynamic Binding Support**: Bind translation keys dynamically using data binding
- **Placeholder Support**: Include dynamic content in translations using placeholders
- **Resource Embedding**: Embed translation files as resources in your assembly

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

**Languages/en-US.csv:**
```csv
// English language translations (en-US)
OtherTextKey;Other Text
BindingLanguageKey;Binding Language Key
Button:ChangeText;Change Text
PartA;Part A
PartB;Part B
```

**Languages/de-DE.csv:**
```csv
// German language translations (de-DE)
OtherTextKey;Anderer Text
BindingLanguageKey;Bindungssprachschlüssel
Button:ChangeText;Text ändern
PartA;Teil A
PartB;Teil B
```

### 2. Initialize LocalizationController

In your ViewModel or code-behind, initialize the `LocalizationController` and load your translation files:

```csharp
using ErikForwerk.Localization.WPF.Models;
using ErikForwerk.Localization.WPF.Tools;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private LocalizationController _localization = new(Application.Current.MainWindow);

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

Bind the window's language to the selected culture:

```xaml
<Window Language="{Binding SelectedCulture}">
```

#### Static Translation

Use a static translation key directly:

```xaml
<TextBlock Text="{loc:Localization OtherTextKey}" />
```

#### Dynamic Translation with Binding

Bind the translation key dynamically:

```xaml
<TextBlock Text="{loc:Localization {Binding SampleLangKey}}" />
```

In your ViewModel:

```csharp
private string _sampleLangKey = "BindingLanguageKey";

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
```

#### Translation with Placeholders

Use placeholders in your translations that get replaced at runtime:

```xaml
<TextBlock Text="{loc:Localization 'Foobar: %PartA% + %PartB% + %Missing%', ParsePlaceholders=True}" />
```

The placeholders (`%PartA%`, `%PartB%`) will be replaced with their corresponding translations from the CSV file.

#### Language Selector

Create a ComboBox to let users switch languages:

```xaml
<ComboBox
    DisplayMemberPath="DisplayName"
    ItemsSource="{Binding SupportedCultures}"
    SelectedItem="{Binding SelectedCulture}" />
```

### 4. Handling Missing Translations

If a translation key is not found, the framework will display the key itself, making it easy to identify missing translations:

```xaml
<TextBlock Text="{loc:Localization Blah:Foobar}" />
<!-- Will display "Blah:Foobar" if the key doesn't exist -->
```

## Example Project

For a complete working example, check out the [WpfLocalizationExample](./Examples/WpfLocalizationExample) project included in this repository. It demonstrates:

- Setting up the LocalizationController
- Static and dynamic translations
- Placeholder replacement
- Runtime language switching with a ComboBox
- Multiple language support (English, German, Russian, Japanese)

## Features

- ✅ Simple XAML markup extension
- ✅ CSV-based translation files
- ✅ Runtime language switching
- ✅ Static and dynamic translation keys
- ✅ Placeholder support for dynamic content
- ✅ Embedded resource support
- ✅ Support for any CultureInfo
- ✅ Automatic culture detection
- ✅ Missing translation detection

## Requirements

- .NET 9.0 or higher
- WPF Application

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
