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

public class ActorEditWindow : EntityEditWindow<ActorEntity> {
	private readonly CustomizeEditorUi _custom;
	private readonly EquipmentEditorUi _equip;

	private IAppearanceManager Editor => this.Context.Appearance;
	
	public ActorEditWindow(
		IEditorContext context,
		CustomizeEditorUi custom,
		EquipmentEditorUi equip
	) : base("Actor Editor", context) {
		this._custom = custom;
		this._equip = equip;
		custom.Editor = this.Editor.Customize;
		equip.Editor = this.Editor.Equipment;
	}

	// Draw tabs

	public override void OnOpen() {
		this._custom.Setup();
	}

	public override void PreDraw() {
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(540, 380),
			MaximumSize = ImGui.GetIO().DisplaySize * 0.90f
		};
	}
	
	public override void Draw() {
		using var _ = ImRaii.TabBar("##ActorEditTabs");
		this.DrawTab("Appearance", this.DrawCustomize);
		this.DrawTab("Equipment", this.DrawEquipment);
	}

	private void DrawTab(string name, Action draw) {
		using var tab = ImRaii.TabItem(name);
		if (tab.Success) draw.Invoke();
	}
	
	// Customize

	private void DrawCustomize() {
		this._custom.Draw(this.Target);
	}
	
	// Equipment

	private void DrawEquipment() {
		this._equip.Draw(this.Target);
	}
}
