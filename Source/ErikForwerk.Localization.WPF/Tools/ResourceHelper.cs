
using System.IO;																	// Stream, ...
using System.Reflection;															// Assembly
using System.Windows;																//Application

namespace ErikForwerk.Localization.WPF.Tools;

public static class ResourceHelper
{
	/// <summary>
	/// Retrieves the text content of an embedded application resource specified by its path.
	/// </summary>
	/// <remarks>
	/// The resource must be included in the application package with its build action set to "Resource".
	/// This method uses the pack URI scheme to locate resources at runtime.
	/// The method automatically prepends a forward slash to the path if it is missing.
	/// Returns null if the resource does not exist or cannot be read.
	/// </remarks>
	/// <param name="resourcePath">
	/// The relative path to the resource within the application package. Must not be null.
	/// If the path does not begin with a '/', one will be prepended automatically.
	/// </param>
	/// <returns>A string containing the full text content of the resource, or null if the resource cannot be found.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="resourcePath"/> is null or whitespace.</exception>"
	public static string? GetResourceText(string resourcePath, Assembly? assembly = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(resourcePath);

		resourcePath = resourcePath.TrimStart('/');

		//--- Build Pack URI ---
		string packUri = (assembly is null)
			? $"pack://application:,,,/{resourcePath}"
			: $"pack://application:,,,/{assembly.GetName().Name};component/{resourcePath}";

		//--- Geht: Dateieigenschaft [Build-Vorgang = "Resource"] einstellen (standard) --------
		Console.WriteLine($"Loading resource at path [{resourcePath}] using Pack-URI [{packUri}]...");
		Console.WriteLine($"Current Thread Culture:  [{Thread.CurrentThread.CurrentCulture.Name}]");
		Console.WriteLine($"Current UI Culture:      [{Thread.CurrentThread.CurrentUICulture.Name}]");

		Stream stream = Application.GetResourceStream(new Uri(packUri)).Stream;

		using StreamReader reader = new(stream);
		return reader.ReadToEnd();
	}
}