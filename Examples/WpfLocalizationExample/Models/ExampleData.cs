
namespace WpfLocalizationExample.Models;

public sealed class ExampleData
{
	public EExampleEnum EnumValue
		{ get; set; } = EExampleEnum.SecondValue;

	public string StringValue
		{ get; set; } = "BindingLanguageKey";

	public int IntValue
		{ get; set; } = 42;

	public double DoubleValue
		{ get; set; } = 3.14;

	public bool BoolValue
		{ get; set; } = true;

	public override string ToString()
		=> $"{EnumValue}-{IntValue}";
}
