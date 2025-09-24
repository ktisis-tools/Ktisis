using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using GLib.Widgets;

using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Data.Config.Sections;
using Ktisis.Localization;

namespace Ktisis.Interface.Components.Config;

[Transient]
public class PresetEditor {
	private readonly ConfigManager _cfg;
	private readonly LocaleManager _locale;

	private PresetConfig Config => this._cfg.File.Presets;

	private const uint ColorYellow = 0xFF00FFFF;
	
	public PresetEditor(
		ConfigManager cfg,
		LocaleManager locale
	) {
		this._cfg = cfg;
		this._locale = locale;
	}

	public void Setup() {
		this.Selected = null;
	}

	private string? Selected = null;
	private string PresetName = null;
	private bool IsDefault = false;
	
	public void Draw() {
		ImGui.Text(this._locale.Translate("config.presets.description"));
		ImGui.Text(this._locale.Translate("config.presets.defaults"));
		ImGui.Spacing();

		using var tablePad = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(10, 10));
		using var table = ImRaii.Table("##PresetsTable", 2, ImGuiTableFlags.Resizable);
		
		if (!table.Success) return;
			
		ImGui.TableSetupColumn("PresetList");
		ImGui.TableSetupColumn("PresetOptions");
		ImGui.TableNextRow();

		this.DrawPresetList();
		this.DrawPresetConfig();
	}
	
	private void DrawPresetList() {
		ImGui.TableNextColumn();
		
		using var _ = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, UiBuilder.DefaultFont.FontSize);
		foreach (var (name, bones) in Config.Presets) {
			var flags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAvailWidth;
			var isDefault = Config.PresetIsDefault(name);

			if (isDefault)
				flags |= ImGuiTreeNodeFlags.Bullet;

			if (Selected == name)
				flags |= ImGuiTreeNodeFlags.Selected;

			using var c = ImRaii.PushColor(ImGuiCol.Text, isDefault ? ColorYellow : ImGui.GetColorU32(ImGuiCol.Text));
			using var node = ImRaii.TreeNode(name, flags);
			
			if (ImGui.IsItemClicked(ImGuiMouseButton.Left)) {
				this.Selected = this.Selected != name ? name : null;
				this.PresetName = name;
				this.IsDefault = isDefault;
			} else if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
				if (isDefault) {
					Config.DefaultPresets.Remove(name);
					this.IsDefault = false;
				}
				else {
					Config.DefaultPresets.Add(name);
					this.IsDefault = true;
				}
			}
		}
	}

	private void DrawPresetConfig() {
		ImGui.TableNextColumn();
		if (Selected == null) return;

		ImGui.InputText("##Rename", ref this.PresetName);
		var isValid = this.PresetName.Length > 0;
		if (isValid && ImGui.IsKeyPressed(ImGuiKey.Enter) && ImGui.IsItemDeactivated()) Rename();
		
		ImGui.SameLine();
		if (ImGui.Button(this._locale.Translate("config.presets.rename"))) Rename();

		using (ImRaii.Disabled(!ImGui.IsKeyDown(ImGuiKey.ModShift))) {
			if (ImGui.Button(this._locale.Translate("config.presets.delete"))) Delete();
			
			if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
				using var _ = ImRaii.Tooltip();
				ImGui.Text(this._locale.Translate("config.presets.delete_tooltip"));
			}
		}
	}

	private void Rename() {
		if (Selected is null) return;
		if (Selected == PresetName) return;

		var preset = Config.Presets[Selected];
		Config.Presets[PresetName] = preset;
		if (IsDefault)
			Config.DefaultPresets.Add(PresetName);
		Config.Presets.Remove(Selected);
		Config.DefaultPresets.Remove(Selected);
		Selected = PresetName;
	}

	private void Delete() {
		if (Selected is null) return;

		PresetConfig.PresetRemovedEvent?.Invoke(Selected);
		Config.Presets.Remove(Selected);
		Config.DefaultPresets.Remove(Selected);
		Selected = null;
	}
}
