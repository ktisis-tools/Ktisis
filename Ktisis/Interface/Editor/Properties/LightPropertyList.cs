using System;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;

using GLib.Popups;
using GLib.Widgets;

using Ktisis.Common.Extensions;
using Ktisis.Data.Config.Gobos;
using Ktisis.Data.Serialization;
using Ktisis.Interface.Editor.Properties.Types;
using Ktisis.Localization;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.World;
using Ktisis.Structs.Lights;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Windows.Editors;
using Ktisis.Scene.Modules.Lights;

namespace Ktisis.Interface.Editor.Properties;

public class LightPropertyList : ObjectPropertyList {
	private readonly IEditorContext _ctx;
	private readonly GuiManager _gui;

	public LightPropertyList(
		IEditorContext ctx,
		GuiManager gui
	) {
		this._ctx = ctx;
		this._gui = gui;
	}
	
	public unsafe override void Invoke(IPropertyListBuilder builder, SceneEntity entity) {
		if (entity is not LightEntity light)
			return;
		
		var embedEditor = this._gui.GetOrCreate<LightWindow>(this._ctx);
		embedEditor.SetTarget(light);
		builder.AddHeader(Ktisis.Locale.Translate("object_edit.light.headers.light"), () => embedEditor.DrawLightTab(light));
		builder.AddHeader(Ktisis.Locale.Translate("object_edit.light.headers.shadow"), () => embedEditor.DrawShadowsTab(light));
	}
}
