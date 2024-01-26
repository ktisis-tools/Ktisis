using System;

using Dalamud.Configuration;

using Ktisis.Data.Config.Actions;
using Ktisis.Data.Config.Sections;

namespace Ktisis.Data.Config;

[Serializable]
public class Configuration : IPluginConfiguration {
	public const int CurrentVersion = 5;
	public int Version { get; set; } = CurrentVersion;

	public CategoryConfig Categories = new();
	public EditorConfig Editor = new();
	public FileConfig File = new();
	public GizmoConfig Gizmo = new();
	public InputConfig Keybinds = new();
	public LocaleConfig Locale = new();
	public OverlayConfig Overlay = new();
}
