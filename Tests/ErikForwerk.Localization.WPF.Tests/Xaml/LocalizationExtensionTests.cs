
using System.Windows;
using System.Windows.Data;

using ErikForwerk.Localization.WPF.CoreLogic;
using ErikForwerk.Localization.WPF.Models;

using Moq;

//-----------------------------------------------------------------------------------------------------------------------------------------
namespace ErikForwerk.Localization.WPF.Tests.Xaml;

//-----------------------------------------------------------------------------------------------------------------------------------------
public sealed class LocalizationExtensionTests
{
	//-----------------------------------------------------------------------------------------------------------------
	#region Helper Methods

	private static BindingExpression CreateMockBindingExpression()
	{
		// BindingExpression kann nicht direkt instanziiert werden, daher verwenden wir einen Workaround
		// Für den Test reicht es, dass der Typ erkannt wird
		DependencyObject target			= new();
		Binding binding					= new("TestPath");
		BindingExpression? expression	= BindingOperations.SetBinding(
			target
			, FrameworkElement.DataContextProperty
			, binding
		) as BindingExpression;

		return expression!;
	}

	#endregion Helper Methods

	//-----------------------------------------------------------------------------------------------------------------
	#region Constructor Tests

	[Fact]
	public void Ctor_Default_InitializesWithEmptyKey()
	{
		//--- ARRANGE ---------------------------------------------------------
		//--- ACT -------------------------------------------------------------
		LocalizationExtension uut	= new();

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(string.Empty,	uut.Key);
		Assert.Null(				uut.KeyBinding);
		Assert.False(				uut.ParsePlaceholders);
	}

	[Theory]
	[InlineData("")]
	[InlineData("TestKey")]
	[InlineData("Some.Nested.Key")]
	public void Ctor_WithKey_SetsKeyProperty(string key)
	{
		//--- ARRANGE ---------------------------------------------------------
		//--- ACT -------------------------------------------------------------
		LocalizationExtension uut	= new(key);

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(key,			uut.Key);
		Assert.Null(				uut.KeyBinding);
		Assert.False(				uut.ParsePlaceholders);
	}

	[Fact]
	public void Ctor_WithBinding_SetsKeyBindingProperty()
	{
		//--- ARRANGE ---------------------------------------------------------
		Binding binding				= new("TestPath");

		//--- ACT -------------------------------------------------------------
		LocalizationExtension uut	= new(binding);

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(string.Empty,	uut.Key);
		Assert.Same(binding,		uut.KeyBinding);
		Assert.False(				uut.ParsePlaceholders);
	}

	#endregion Constructor Tests

	//-----------------------------------------------------------------------------------------------------------------
	#region Property Tests

	[Theory]
	[InlineData("Key1")]
	[InlineData("Key2")]
	[InlineData("")]
	public void Key_SetAndGet_ReturnsCorrectValue(string key)
	{
		//--- ARRANGE -------------------------------------------------------------
		LocalizationExtension uut = new()
		{
			//--- ACT -----------------------------------------------------------------
			Key = key
		};

		//--- ASSERT --------------------------------------------------------------
		Assert.Equal(key, uut.Key);
	}

	[Fact]
	public void KeyBinding_SetAndGet_ReturnsCorrectValue()
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationExtension uut	= new();
		Binding binding				= new("TestPath");

		//--- ACT -------------------------------------------------------------
		uut.KeyBinding = binding;

		//--- ASSERT ----------------------------------------------------------
		Assert.Same(binding, uut.KeyBinding);
	}

	[Fact]
	public void KeyBinding_SetToNull_ReturnsNull()
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationExtension uut = new(new Binding("TestPath"))
		{
			//--- ACT -------------------------------------------------------------
			KeyBinding = null
		};

		//--- ASSERT ----------------------------------------------------------
		Assert.Null(uut.KeyBinding);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ParsePlaceholders_SetAndGet_ReturnsCorrectValue(bool value)
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationExtension uut = new()
		{
			//--- ACT ---------------------------------------------------------
			ParsePlaceholders = value
		};

		//--- ASSERT ----------------------------------------------------------
		Assert.Equal(value, uut.ParsePlaceholders);
	}

	#endregion Property Tests

	//-----------------------------------------------------------------------------------------------------------------
	#region ProvideValue Tests - Static Key

	[Fact]
	public void ProvideValue_WithStaticKey_ReturnsBindingWithCorrectConverter()
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationExtension uut	= new("TestKey");

		//--- ACT -------------------------------------------------------------
		object result = uut.ProvideValue(Mock.Of<IServiceProvider>());

		//--- ASSERT ----------------------------------------------------------
		Binding binding = Assert.IsType<Binding>(result);

		Assert.Same(TranslationCoreBindingSource.Instance,	binding.Source);
		Assert.Equal(BindingMode.OneWay,					binding.Mode);
		Assert.NotNull(										binding.Converter);
		_ = Assert.IsType<LocalizationTextConverter>(		binding.Converter);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ProvideValue_WithStaticKeyAndParsePlaceholders_ConfiguresConverterCorrectly(bool parsePlaceholders)
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationExtension uut = new("TestKey")
		{
			ParsePlaceholders = parsePlaceholders
		};

		//--- ACT -------------------------------------------------------------
		object result = uut.ProvideValue(Mock.Of<IServiceProvider>());

		//--- ASSERT ----------------------------------------------------------
		Binding binding = Assert.IsType<Binding>(result);
		Assert.NotNull(binding.Converter);
	}

	[Fact]
	public void ProvideValue_WithEmptyKey_ReturnsBindingWithConverter()
	{
		//--- ARRANGE ---------------------------------------------------------
		LocalizationExtension uut = new(string.Empty);

		//--- ACT -------------------------------------------------------------
		object result = uut.ProvideValue(Mock.Of<IServiceProvider>());

		//--- ASSERT ----------------------------------------------------------
		Binding binding = Assert.IsType<Binding>(result);
		Assert.NotNull(binding.Converter);
	}

	#endregion ProvideValue Tests - Static Key

	//-----------------------------------------------------------------------------------------------------------------
	#region ProvideValue Tests - Dynamic Binding

	[Fact]
	public void ProvideValue_WithBinding_ReturnsMultiBinding()
	{
		//--- ARRANGE ---------------------------------------------------------
		Binding keyBinding				= new("DynamicKey");
		LocalizationExtension uut		= new(keyBinding);

		//--- ACT -------------------------------------------------------------
		object result = uut.ProvideValue(Mock.Of<IServiceProvider>());

		//--- ASSERT ----------------------------------------------------------
		MultiBinding multiBinding = Assert.IsType<MultiBinding>(result);

		Assert.Equal(2, multiBinding.Bindings.Count);
		Assert.NotNull(multiBinding.Converter);

		_ = Assert.IsType<LocalizationDynamicTextConverter>(multiBinding.Converter);
	}

	[Fact]
	public void ProvideValue_WithBindingExpression_ReturnsMultiBinding()
	{
		//--- ARRANGE ---------------------------------------------------------
		// Simulieren eines BindingExpression-Szenarios durch direktes Setzen der KeyBinding-Property
		BindingExpression bindingExpression	= CreateMockBindingExpression();
		LocalizationExtension uut			= new(bindingExpression);

		//--- ACT -------------------------------------------------------------
		object result = uut.ProvideValue(Mock.Of<IServiceProvider>());

		//--- ASSERT ----------------------------------------------------------
		_ = Assert.IsType<MultiBinding>(result);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ProvideValue_WithBindingAndParsePlaceholders_ConfiguresConverterParameterCorrectly(bool parsePlaceholders)
	{
		//--- ARRANGE ---------------------------------------------------------
		const string PATH			= "TestPath";
		Binding keyBinding			= new(PATH);
		LocalizationExtension uut	= new(keyBinding)
		{
			ParsePlaceholders = parsePlaceholders
		};

		//--- ACT -------------------------------------------------------------
		object result				= uut.ProvideValue(Mock.Of<IServiceProvider>());

		//--- ASSERT ----------------------------------------------------------
		MultiBinding multiBinding			= Assert.IsType<MultiBinding>(result);
		PlaceholderConverterParameter param	= Assert.IsType<PlaceholderConverterParameter>(multiBinding.ConverterParameter);

		Assert.Equal(PATH,				param.DesignTimeFallback);
		Assert.Equal(parsePlaceholders,	param.ParsePlaceholders);
	}

	[Fact]
	public void ProvideValue_WithBindingWithoutPath_CreatesValidMultiBinding()
	{
		//--- ARRANGE ---------------------------------------------------------
		Binding keyBinding			= new();
		LocalizationExtension uut	= new(keyBinding);

		//--- ACT -------------------------------------------------------------
		object result = uut.ProvideValue(Mock.Of<IServiceProvider>());

		//--- ASSERT ----------------------------------------------------------
		MultiBinding multiBinding = Assert.IsType<MultiBinding>(result);

		Assert.Equal(2,		multiBinding.Bindings.Count);
		Assert.NotNull(		multiBinding.Converter);
		Assert.NotNull(		multiBinding.ConverterParameter);
	}

	#endregion ProvideValue Tests - Dynamic Binding

	//-----------------------------------------------------------------------------------------------------------------
	#region ProvideValue Tests - Other Cases

	[Fact]
	public void ProvideValue_WithNullKeyBinding_ReturnsSingleBinding()
	{
		//--- ARRANGE ---------------------------------------------------------
		const string TEST_KEY		= "TestKey";
		LocalizationExtension uut	= new(TEST_KEY)
		{
			KeyBinding = null
		};

		//--- ACT -------------------------------------------------------------
		object result = uut.ProvideValue(Mock.Of<IServiceProvider>());

		//--- ASSERT ----------------------------------------------------------
		_ = Assert.IsType<Binding>(result);
	}

	[Fact]
	public void ProvideValue_WithNonBindingObject_ReturnsSingleBinding()
	{
		//--- ARRANGE ---------------------------------------------------------
		const string TEST_KEY		= "TestKey";
		object someObject			= new();
		LocalizationExtension uut	= new(TEST_KEY)
		{
			KeyBinding = someObject
		};

		//--- ACT -------------------------------------------------------------
		object result = uut.ProvideValue(Mock.Of<IServiceProvider>());

		//--- ASSERT ----------------------------------------------------------
		_ = Assert.IsType<Binding>(result);
	}

	#endregion ProvideValue Tests - Other Cases
}
