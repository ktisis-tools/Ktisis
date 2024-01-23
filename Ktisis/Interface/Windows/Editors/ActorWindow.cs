using System;
using System.Numerics;

using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

using Ktisis.Editor.Characters.Types;
using Ktisis.Editor.Context;
using Ktisis.Interface.Components.Actors;
using Ktisis.Interface.Types;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Windows.Editors;

public class ActorWindow : EntityEditWindow<ActorEntity> {
	private readonly CustomizeEditorUi _custom;
	private readonly EquipmentEditorUi _equip;

	private IAppearanceManager Manager => this.Context.Appearance;
	
	public ActorWindow(
		IEditorContext context,
		CustomizeEditorUi custom,
		EquipmentEditorUi equip
	) : base("Actor Editor", context) {
		this._custom = custom;
		this._equip = equip;
	}
	
	// Target

	public override void SetTarget(ActorEntity target) {
		base.SetTarget(target);
		
		var customize = this.Manager.GetCustomizeEditor(target);
		this._custom.Editor = customize;
		
		this._equip.Editor = this.Manager.GetEquipmentEditor(target);
	}

	// Draw tabs

	public override void OnOpen() {
		this._custom.Setup();
	}

	public override void PreDraw() {
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(560, 380),
			MaximumSize = ImGui.GetIO().DisplaySize * 0.90f
		};
	}
	
	public override void Draw() {
		using var _ = ImRaii.TabBar("##ActorEditTabs");
		DrawTab("Appearance", this._custom.Draw);
		DrawTab("Equipment", this._equip.Draw);
	}

	private static void DrawTab(string name, Action draw) {
		using var tab = ImRaii.TabItem(name);
		if (tab.Success) draw.Invoke();
	}
}
