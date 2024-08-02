using System;

using Dalamud.Configuration;

using Ktisis.Data.Config.Actions;
using Ktisis.Data.Config.Entity;
using Ktisis.Data.Config.Sections;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Data.Config;

[Serializable]
public class Configuration : IPluginConfiguration {
	public const int CurrentVersion = 10;
	public int Version { get; set; } = CurrentVersion;

	public CategoryConfig Categories = new();
	public EditorConfig Editor = new();
	public FileConfig File = new();
	public GizmoConfig Gizmo = new();
	public InputConfig Keybinds = new();
	public LocaleConfig Locale = new();
	public OverlayConfig Overlay = new();
	public AutoSaveConfig AutoSave = new();

	public EntityDisplay GetEntityDisplay(SceneEntity entity) {
		var display = this.Editor.GetDisplayForType(entity.Type);
		return entity switch {
			BoneNodeGroup { Category: { } category } => display with { Color = category.GroupColor },
			BoneNode { Parent: BoneNodeGroup { Category: { } category } } => display with { Color = category.LinkedColors ? category.GroupColor : category.BoneColor },
			_ => display
		};
	}
}
