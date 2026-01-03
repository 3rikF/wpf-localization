# WPF Localization Framework

A lightweight and easy-to-use localization framework for WPF/XAML applications that enables runtime language switching with minimal configuration.

## Current Status

[![Current Repository Status](https://github.com/3rikF/wpf-localization/actions/workflows/dotnet_build-test-pack-publish.yml/badge.svg)](https://github.com/3rikF/wpf-localization/actions) 
[![Codecov Test Coverage](https://codecov.io/gh/3rikF/wpf-localization/graph/badge.svg?token=6DBLGNQC73)](https://codecov.io/gh/3rikF/wpf-localization) 
[![wakatime](https://wakatime.com/badge/user/ccce5eac-49f0-481f-998c-1183a3cd0b18/project/6d16b00f-2c72-4608-9b1d-047225a31992.svg)](https://wakatime.com/badge/user/ccce5eac-49f0-481f-998c-1183a3cd0b18/project/6d16b00f-2c72-4608-9b1d-047225a31992) 
[![NuGet](https://img.shields.io/nuget/v/ErikForwerk.Localization.WPF)](https://www.nuget.org/packages/ErikForwerk.Localization.WPF/) 

## Table of Contents

- [⚠️ Breaking Changes in v0.3.0](#️-breaking-changes-in-v030)
- [Features](#features)
- [Installation](#installation)
- [Usage](#usage)
  - [1. Create Translation Files](#1-create-translation-files)
  - [2. Initialize LocalizationController](#2-initialize-localizationcontroller)
  - [3. Use in XAML](#3-use-in-xaml)
  - [4. Handling Missing Translations](#4-handling-missing-translations)
- [Example Project](#example-project)
- [Requirements](#requirements)
- [License](#license)
- [Contributing](#contributing)

## ⚠️ Breaking Changes in v0.3.0

**Important:** If you are upgrading from v0.2.2 or earlier, please note the following breaking changes:

1. **LocalizationController Constructor**: The constructor no longer requires a `Window` parameter. Change from:
   ```csharp
   // Old (v0.2.2 and earlier)
   private LocalizationController _localization = new(System.Windows.Application.Current.MainWindow);
   ```
   to:
   ```csharp
   // New (v0.3.0+)
   private LocalizationController _localization = new();
   ```

2. **Language Synchronization**: Instead of passing a Window reference to the constructor, use the new `LocalizationBehavior.SyncLanguage` attached property in XAML:
   ```xaml
   <Window ... loc:LocalizationBehavior.SyncLanguage="True">
   ```
   This automatically synchronizes the element's language with the current localization culture.

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
    private LocalizationController _localization = new();

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
xmlns:loc="clr-namespace:ErikForwerk.Localization.WPF.Xaml;assembly=ErikForwerk.Localization.WPF"
```

#### Enable Automatic Language Synchronization

To automatically synchronize the language of a Window or any FrameworkElement with the current localization culture, use the `LocalizationBehavior.SyncLanguage` attached property:

```xaml
<Window ... loc:LocalizationBehavior.SyncLanguage="True">
```

This behavior:
- Automatically updates the element's `Language` property when the culture changes
- Handles cleanup on element unload to prevent memory leaks
- Can be applied to any `FrameworkElement`, not just windows

**Example:**

```xaml
<Window
    x:Class="MyApp.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:loc="clr-namespace:ErikForwerk.Localization.WPF.Xaml;assembly=ErikForwerk.Localization.WPF"
    loc:LocalizationBehavior.SyncLanguage="True"
    Title="MainWindow">
    <!-- Your content here -->
</Window>
```

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

Create a ComboBox to let users switch languages. The `SupportedCultures` and `SelectedCulture` properties should be exposed in your ViewModel using the `LocalizationController` instance (see step 2 above):

```xaml
<ComboBox
    DisplayMemberPath	="DisplayName"
    ItemsSource			="{Binding SupportedCultures}"
    SelectedItem		="{Binding SelectedCulture}" />
```

The bindings connect to properties in your ViewModel that wrap the `LocalizationController.SupportedCultures` and `LocalizationController.CurrentCulture` properties. See the [Example Project](#example-project) for a complete implementation.

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
