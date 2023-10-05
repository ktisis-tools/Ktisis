using System.Collections.Generic;

using Dalamud.Configuration;

using Ktisis.Config.Bones;
using Ktisis.Config.Display;
using Ktisis.Config.Input;
using Ktisis.ImGuizmo;
using Ktisis.Editing;

namespace Ktisis.Config; 

public class ConfigFile : IPluginConfiguration {
	// Version

	public const int CurrentVersion = 4;

	public int Version { get; set; } = CurrentVersion;
	
	// Localization

	public string LocaleId = "en_US";
	
	// Input#

	public bool Keybinds_Active = true;

	public Dictionary<string, Keybind> Keybinds = new();
	
	// Overlay

	public bool Overlay_Gizmo = true;
	public bool Overlay_Visible = true;
	
	// Gizmo

	public Mode Gizmo_Mode = Mode.Local;
	public Operation Gizmo_Op = Operation.ROTATE;

	// Transform Editor
	
	public bool Editor_Gizmo = true;
	public bool Editor_OpenOnSelect = true;
	
	public EditMode Editor_Mode = EditMode.Object;
	public EditFlags Editor_Flags = EditFlags.Propagate;
	
	// Item display
	
	public Categories Categories = new() {
		Default = new BoneCategory("Other")
	};

	public Dictionary<ItemType, ItemDisplay> Display = ItemDisplay.GetDefaults();
}
