using System;
using System.Numerics;

using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;

using Ktisis.Editor.Animation.Types;
using Ktisis.Editor.Characters.Types;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Components.Chara;
using Ktisis.Interface.Types;
using Ktisis.Scene.Entities.Game;
using Ktisis.Structs.Actors;
using Ktisis.Structs.Characters;
using Ktisis.Interface.Components.Chara.Select;
using Ktisis.GameData.Excel.Types;

namespace Ktisis.Interface.Windows.Editors;

public class ActorWindow : EntityEditWindow<ActorEntity> {
	private const string WindowId = "KtisisActorEditor";
	
	private readonly CustomizeEditorTab _custom;
	private readonly EquipmentEditorTab _equip;
	private readonly AnimationEditorTab _anim;
	private readonly PluginDataEditorTab _ipc;

	private IAnimationManager Animation => this.Context.Animation;
	private ICharacterManager Manager => this.Context.Characters;

	private readonly NpcSelect _npcs;
	
	public ActorWindow(
		IEditorContext ctx,
		CustomizeEditorTab custom,
		EquipmentEditorTab equip,
		AnimationEditorTab anim,
		NpcSelect npcs,
		IDalamudPluginInterface dpi
	) : base($"Actor Editor###{WindowId}", ctx) {
		this._custom = custom;
		this._equip = equip;
		this._anim = anim;
		this._ipc = new PluginDataEditorTab(ctx, dpi);
		this._npcs = npcs;
		this._npcs.OnSelected += this.OnNpcSelect;
	}

	public override void PreOpenCheck() {
		if (this.Context.IsValid) return;
		Ktisis.Log.Verbose("Context for actor window is stale, closing...");
		this.Close();
	}
	
	// Target

	private ICustomizeEditor _editCustom = null!;

	public override void SetTarget(ActorEntity target) {
		this.WindowName = $"Actor Editor - {target.Name}###{WindowId}";
		
		base.SetTarget(target);
		
		this._editCustom = this._custom.Editor = this.Manager.GetCustomizeEditor(target);
		this._equip.Editor = this.Manager.GetEquipmentEditor(target);
		this._anim.Editor = this.Animation.GetAnimationEditor(target);
		this._ipc.SetTarget(target);
		this._anim.ClearPoseExpression();
	}

	// Draw tabs

	public override void OnOpen() {
		this._custom.Setup(this.Context);
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
		this.UpdateTarget();
		
		using var _ = ImRaii.TabBar("##ActorEditTabs");
		DrawTab(Ktisis.Locale.Translate("chara_edit.animation.tab"), this._anim.Draw);
		DrawTab(Ktisis.Locale.Translate("chara_edit.customize.tab"), this._custom.Draw);
		DrawTab(Ktisis.Locale.Translate("chara_edit.equip.tab"), this._equip.Draw);
		DrawTab(Ktisis.Locale.Translate("chara_edit.ipc.tab"), this._ipc.Draw);
		DrawTab(Ktisis.Locale.Translate("chara_edit.misc.tab"), this.DrawMisc);
	}

	private static void DrawTab(string name, Action draw) {
		using var tab = ImRaii.TabItem(name);
		if (tab.Success) draw.Invoke();
	}
	
	// Advanced tab

	private unsafe void DrawMisc() {
		var space = ImGui.GetStyle().ItemInnerSpacing.X;
		ImGui.Spacing();
		
		var modelId = (int)this._editCustom.GetModelId();
		if (ImGui.InputInt(Ktisis.Locale.Translate("chara_edit.misc.model"), ref modelId, flags: ImGuiInputTextFlags.EnterReturnsTrue))
			this._editCustom.SetModelId((uint)modelId);
		if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
			using var _ = ImRaii.Tooltip();
			ImGui.Text(Ktisis.Locale.Translate("chara_edit.misc.model_tip"));
		}

		ImGui.SameLine(0, space);
		this._npcs.DrawSearchIcon();

		var chara = (CharacterEx*)this.Target.Character;
		if (chara != null) {
			ImGui.Spacing();
			ImGui.SliderFloat(Ktisis.Locale.Translate("chara_edit.misc.opacity"), ref chara->Opacity, 0.0f, 1.0f);
		}
		
		ImGui.Spacing();
		ImGui.Spacing();

		this.DrawWetness();
	}
	
	// Wetness

	private void DrawWetness() {
		var isWetActive = this.Target.Appearance.Wetness != null;
		if (ImGui.Checkbox(Ktisis.Locale.Translate("chara_edit.misc.wetness"), ref isWetActive))
			this.ToggleWetness();

		var wetness = this.GetWetness();
		if (wetness == null) return;
		
		using var _ = ImRaii.Disabled(!isWetActive);
		ImGui.Spacing();

		var changed = false;
		var values = (WetnessState)wetness;
		changed |= ImGui.SliderFloat(Ktisis.Locale.Translate("chara_edit.misc.wetness.weather"), ref values.WeatherWetness, 0.0f, 1.0f);
		changed |= ImGui.SliderFloat(Ktisis.Locale.Translate("chara_edit.misc.wetness.swim"), ref values.SwimmingWetness, 0.0f, 1.0f);
		changed |= ImGui.SliderFloat(Ktisis.Locale.Translate("chara_edit.misc.wetness.depth"), ref values.WetnessDepth, 0.0f, 3.0f);
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

	private void OnNpcSelect(INpcBase npc) => this._editCustom.SetModelId((uint)npc.GetModelId());
}
