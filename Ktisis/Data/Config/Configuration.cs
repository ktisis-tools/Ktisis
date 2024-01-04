using Dalamud.Configuration;

using Ktisis.Data.Config.Sections;

namespace Ktisis.Data.Config;

public class Configuration : IPluginConfiguration {
	public const int CurrentVersion = 5;
	public int Version { get; set; } = CurrentVersion;

	public CategoryConfig Categories = new();
	public EditorConfig Editor = new();
	public GizmoConfig Gizmo = new();
	public LocaleConfig Locale = new();
}
