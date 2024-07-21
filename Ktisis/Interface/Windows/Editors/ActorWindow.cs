using System;
using System.Numerics;

using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

using Ktisis.Editor.Animation.Types;
using Ktisis.Editor.Characters.Types;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Components.Chara;
using Ktisis.Interface.Types;
using Ktisis.Scene.Entities.Game;
using Ktisis.Structs.Actors;
using Ktisis.Structs.Characters;

namespace Ktisis.Interface.Windows.Editors;

public class ActorWindow : EntityEditWindow<ActorEntity> {
	private readonly CustomizeEditorTab _custom;
	private readonly EquipmentEditorTab _equip;
	private readonly AnimationEditorTab _anim;

	private IAnimationManager Animation => this.Context.Animation;
	private ICharacterManager Manager => this.Context.Characters;
	
	public ActorWindow(
		IEditorContext ctx,
		CustomizeEditorTab custom,
		EquipmentEditorTab equip,
		AnimationEditorTab anim
	) : base("Actor Editor", ctx) {
		this._custom = custom;
		this._equip = equip;
		this._anim = anim;
	}
	
	// Target

	private ICustomizeEditor _editCustom = null!;

	public override void SetTarget(ActorEntity target) {
		base.SetTarget(target);
		
		this._editCustom = this._custom.Editor = this.Manager.GetCustomizeEditor(target);
		this._equip.Editor = this.Manager.GetEquipmentEditor(target);
		this._anim.Editor = this.Animation.GetAnimationEditor(target);
	}

	// Draw tabs

	public override void OnOpen() {
		this._custom.Setup();
		this._anim.Setup();
	}

	public override void PreDraw() {
		base.PreDraw();
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(560, 380),
			MaximumSize = ImGui.GetIO().DisplaySize * 0.90f
		};
	}
	
	public override void Draw() {
		using var _ = ImRaii.TabBar("##ActorEditTabs");
		DrawTab("Appearance", this._custom.Draw);
		DrawTab("Equipment", this._equip.Draw);
		DrawTab("Animation", this._anim.Draw);
		DrawTab("Misc", this.DrawMisc);
	}

	private static void DrawTab(string name, Action draw) {
		using var tab = ImRaii.TabItem(name);
		if (tab.Success) draw.Invoke();
	}
	
	// Advanced tab

	private unsafe void DrawMisc() {
		ImGui.Spacing();
		
		var modelId = (int)this._editCustom.GetModelId();
		if (ImGui.InputInt("Model ID", ref modelId))
			this._editCustom.SetModelId((uint)modelId);

		var chara = (CharacterEx*)this.Target.Character;
		if (chara != null) {
			ImGui.Spacing();
			ImGui.SliderFloat("Opacity", ref chara->Opacity, 0.0f, 1.0f);
		}
		
		ImGui.Spacing();
		ImGui.Spacing();

		this.DrawWetness();
	}
	
	// Wetness

	private void DrawWetness() {
		var isWetActive = this.Target.Appearance.Wetness != null;
		if (ImGui.Checkbox("Wetness Override", ref isWetActive))
			this.ToggleWetness();

		var wetness = this.GetWetness();
		if (wetness == null) return;
		
		using var _ = ImRaii.Disabled(!isWetActive);
		ImGui.Spacing();

		var changed = false;
		var values = (WetnessState)wetness;
		changed |= ImGui.SliderFloat("Weather Wetness", ref values.WeatherWetness, 0.0f, 1.0f);
		changed |= ImGui.SliderFloat("Swimming Wetness", ref values.SwimmingWetness, 0.0f, 1.0f);
		changed |= ImGui.SliderFloat("Wetness Depth", ref values.WetnessDepth, 0.0f, 3.0f);
		if (changed) this.Target.Appearance.Wetness = values;
	}

	private unsafe WetnessState? GetWetness() {
		if (this.Target.Appearance.Wetness is {} value)
			return value;
		var chara = this.Target.CharacterBaseEx;
		return chara != null ? chara->Wetness : null;
	}

	private unsafe void ToggleWetness() {
		var state = this.Target.Appearance;
		if (state.Wetness != null) {
			state.Wetness = null;
		} else {
			var chara = this.Target.CharacterBaseEx;
			state.Wetness = chara != null ? chara->Wetness : null;
		}
	}
}
