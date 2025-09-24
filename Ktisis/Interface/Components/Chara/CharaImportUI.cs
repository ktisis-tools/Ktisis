using System;

using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

using Ktisis.Core.Attributes;
using Ktisis.Data.Files;
using Ktisis.Editor.Characters;
using Ktisis.Editor.Context.Types;
using Ktisis.GameData.Excel.Types;
using Ktisis.Interface.Components.Chara.Select;
using Ktisis.Interface.Components.Files;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Components.Chara;

public enum LoadMethod {
	File,
	Npc
}

[Transient]
public class CharaImportUI {
	public IEditorContext Context { set; private get; } = null!;

	public Action<CharaImportUI>? OnNpcSelected;
	
	private readonly NpcSelect _npcs;
	private readonly FileSelect<CharaFile> _select;
	private bool _isInit = false;

	public CharaImportUI(
		NpcSelect npcs,
		FileSelect<CharaFile> select
	) {
		this._npcs = npcs;
		this._npcs.OnSelected += this.OnNpcSelect;
		this._select = select;
		this._select.OnOpenDialog += this.OnFileDialogOpen;
	}
	
	// Events

	private void OnNpcSelect(INpcBase _) {
		if (this.Context.Config.File.ImportNpcApplyOnSelect)
			this.OnNpcSelected?.Invoke(this);
	}

	private void OnFileDialogOpen(FileSelect<CharaFile> sender) {
		this.Context.Interface.OpenCharaFile(sender.SetFile);
	}
	
	// State

	public LoadMethod Method { get; set; } = LoadMethod.File;
	
	public bool HasSelection => this.Method switch {
		LoadMethod.File => this._select.IsFileOpened,
		LoadMethod.Npc => this._npcs.Selected != null,
		_ => false
	};

	private bool DisableModes => this.Method switch {
		LoadMethod.File => !this.HasSelection,
		LoadMethod.Npc => !this.HasSelection && !this.Context.Config.File.ImportNpcApplyOnSelect,
		_ => false
	};
	
	// Apply selection

	public void ApplyTo(ActorEntity actor) {
		switch (this.Method) {
			case LoadMethod.File:
				this.ApplyCharaFile(actor);
				break;
			case LoadMethod.Npc:
				this.ApplyNpc(actor);
				break;
			default:
				throw new ArgumentOutOfRangeException(this.Method.ToString());
		}
	}
	
	private void ApplyCharaFile(ActorEntity actor) {
		var file = this._select.Selected?.File;
		if (file != null)
			this.Context.Characters.ApplyCharaFile(actor, file, this.Context.Config.File.ImportCharaModes);
	}

	public void ApplyNpc(ActorEntity actor) {
		var npc = this._npcs.Selected;
		if (npc != null)
			this.Context.Characters.ApplyNpc(actor, npc, this.Context.Config.File.ImportCharaModes);
	}
	
	// Importing

	public void DrawImport() {
		switch (this.Method) {
			case LoadMethod.File:
				this._select.Draw();
				break;
			case LoadMethod.Npc:
				this._npcs.Draw();
				ImGui.Spacing();
				ImGui.Checkbox("Apply on selection", ref this.Context.Config.File.ImportNpcApplyOnSelect);
				break;
			default:
				throw new ArgumentOutOfRangeException(this.Method.ToString());
		}
	}
	
	public void DrawLoadMethods(float cursorY = -1.0f) {
		var setCursorY = cursorY > -1.0f;
		if (setCursorY) ImGui.SetCursorPosY(cursorY);
		this.DrawMethodRadio("File", LoadMethod.File);
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		if (setCursorY) ImGui.SetCursorPosY(cursorY);
		this.DrawMethodRadio("NPC", LoadMethod.Npc);
	}
	
	private void DrawMethodRadio(string label, LoadMethod method) {
		if (ImGui.RadioButton(label, this.Method == method))
			this.Method = method;
	}
	
	// Mode selection
	
	public void DrawModesSelect() {
		using var _ = ImRaii.Disabled(this.DisableModes);
		ImGui.Text("Appearance");
		this.DrawModeSwitch("Body", SaveModes.AppearanceBody);
		ImGui.SameLine();
		this.DrawModeSwitch("Face", SaveModes.AppearanceFace);
		ImGui.SameLine();
		this.DrawModeSwitch("Hair", SaveModes.AppearanceHair);

		ImGui.Spacing();

		ImGui.Text("Equipment");
		this.DrawModeSwitch("Gear", SaveModes.EquipmentGear);
		ImGui.SameLine();
		this.DrawModeSwitch("Accessories", SaveModes.EquipmentAccessories);
		ImGui.SameLine();
		this.DrawModeSwitch("Weapons", SaveModes.EquipmentWeapons);
	}
	
	private void DrawModeSwitch(string label, SaveModes mode) {
		var enabled = this.Context.Config.File.ImportCharaModes.HasFlag(mode);
		if (ImGui.Checkbox($"{label}##CharaImportDialog_{mode}", ref enabled))
			this.Context.Config.File.ImportCharaModes ^= mode;
	}
}
