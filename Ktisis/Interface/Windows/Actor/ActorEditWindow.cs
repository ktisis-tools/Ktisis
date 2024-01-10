using System;

using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

using Ktisis.Editor.Context;
using Ktisis.Interface.Components.Actors;
using Ktisis.Interface.Types;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Windows.Actor;

public class ActorEditWindow : KtisisWindow {
	private readonly IEditorContext _context;
	private readonly CustomizeEditor _custom;
	private readonly EquipmentEditor _equip;

	public ActorEntity Target { get; set; } = null!;
	
	public ActorEditWindow(
		IEditorContext context,
		CustomizeEditor custom,
		EquipmentEditor equip
	) : base("Actor Editor") {
		this._context = context;
		this._custom = custom;
		this._equip = equip;
	}

	public override void PreDraw() {
		if (this._context.IsValid && this.Target.IsValid) return;
		Ktisis.Log.Verbose("Context for actor window is stale, closing...");
		this.Close();
	}

	// Draw tabs
	
	public override unsafe void Draw() {
		using var _ = ImRaii.TabBar("##ActorEditTabs");
		this.DrawTab("Appearance", this.DrawCustomize);
		this.DrawTab("Equipment", this.DrawEquipment);
	}

	private void DrawTab(string name, Action draw) {
		using var tab = ImRaii.TabItem(name);
		if (tab.Success) draw.Invoke();
	}
	
	// Customize

	private unsafe void DrawCustomize() {
		var chara = this.Target.GetCharacter();
		if (chara == null) return;

		var custom = this.Target.GetCustomize();
		if (custom != null)
			this._custom.Draw(custom.Value);
	}
	
	// Equipment

	private void DrawEquipment() {
		ImGui.Text("Equip :3");
	}
}
