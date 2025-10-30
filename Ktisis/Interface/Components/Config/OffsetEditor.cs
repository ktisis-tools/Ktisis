using System;
using System.Linq;
using System.Numerics;
using System.Text;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;

using GLib.Widgets;

using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Data.Config.Sections;
using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;
using Ktisis.Localization;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;

using Newtonsoft.Json;

namespace Ktisis.Interface.Components.Config;

[Transient]
public class OffsetEditor {
	private readonly ConfigManager _cfg;
	private readonly ContextManager _ctx;
	private readonly LocaleManager _locale;

	private OffsetConfig Config => this._cfg.File.Offsets;

	public OffsetEditor(
		ConfigManager cfg,
		ContextManager ctx,
		LocaleManager locale
	) {
		this._cfg = cfg;
		this._ctx = ctx;
		this._locale = locale;
	}

	public void Setup() {
		this.UpdateContext();
		// set a default skeleton to view to the first entry
		if (this.Config.BoneOffsets.Keys.Count > 0)
			this.SelectedRaceSexId = this.Config.BoneOffsets.Keys.OrderBy(k => k).First();
	}
	private void UpdateContext() {
		this._editorContext = this._ctx.Current;

		// each frame, if we still dont have a race selected but we DO have a context, see if there's a selection to grab race from
		if (this.HasContext && this.SelectedRaceSexId is null)
			this.SetRaceSexIdFromSelection();
	}

	private void SetRaceSexIdFromSelection() {
		if (!this.HasContext) return;
		var target = this._editorContext!.Selection.GetFirstSelected();
		if (
			target switch {
				BoneNode node => node.Pose.Parent,
				BoneNodeGroup group => group.Pose.Parent,
				EntityPose pose => pose.Parent,
				_ => target
			} is ActorEntity actor
		) {
			var raceSex = actor.GetRaceSexId();
			if (raceSex is null) return;
			this.SelectedRaceSexId = actor.GetRaceSexId();
		}
	}

	private IEditorContext? _editorContext;
	private bool HasContext => this._editorContext is not null;
	private string? SelectedRaceSexId;

	public void Draw() {
		this.UpdateContext(); // refresh context to make sure we dont explode when out of gpose
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
		ImGui.Text("This is a test window for offset editing!");
		ImGui.Text("Select and add a bone to get started!");
		ImGui.Spacing();

		if (ImGui.Button("Copy All to Clipboard"))
			this.Config.SaveToClipboard();
		ImGui.SameLine(0, spacing);

		using (ImRaii.Disabled(!ImGui.IsKeyDown(ImGuiKey.ModShift) || !ImGui.IsKeyDown(ImGuiKey.ModCtrl))) {
			if (ImGui.Button("Load All from Clipboard"))
				this.Config.LoadFromClipboard();

			if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
				using var _ = ImRaii.Tooltip();
				ImGui.Text("Warning: This will replace ALL your current offsets.\nHold CTRL+Shift to confirm.");
			}
		}
		ImGui.Spacing();

		this.DrawBoneSelection();
		ImGui.Spacing();

		if (this.Config.BoneOffsets.Keys.Count < 1) return;
		if (this.SelectedRaceSexId is null) return;
		ImGui.Separator();
		ImGui.Spacing();

		this.DrawSkeletonCombo();
		ImGui.Spacing();

		this.DrawBoneOffsets();
	}

	private void DrawBoneSelection() {
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
		string boneDisplay = "None";
		string? infoName = null;
		string? raceSex = null;

		if (this.HasContext) {
			var target = this._editorContext!.Selection.GetFirstSelected();
			if (target is BoneNode { Pose.Parent: ActorEntity a } b) {
				infoName = b.Info.Name;
				boneDisplay = $"{b.Name}{(b.Name != infoName ? " ({0})".Format(infoName) : "")}";

				raceSex = a.GetRaceSexId();
				boneDisplay += $" on {(raceSex is not null ? this._locale.Translate($"config.offsets.race_sex.{raceSex}") : "Invalid")}";
			}
		}

		using (ImRaii.Disabled(
			raceSex is null
			|| infoName is null
			|| (this.Config.BoneOffsets.ContainsKey(raceSex) && this.Config.BoneOffsets[raceSex].ContainsKey(infoName))
		)) if (Buttons.IconButtonTooltip(FontAwesomeIcon.Plus, "Add bone to offsets"))
				this.Config.UpsertOffset(raceSex!, infoName!, new Vector3());

		ImGui.SameLine(0, spacing);
		using (ImRaii.Disabled(
			raceSex is null
			|| infoName is null
			|| this.SelectedRaceSexId == raceSex
			|| !this.Config.BoneOffsets.ContainsKey(raceSex)
		)) {
			if (raceSex is null)
				Buttons.IconButton(FontAwesomeIcon.Eye);
			else if (Buttons.IconButtonTooltip(FontAwesomeIcon.Eye, $"Open offsets for {this._locale.Translate($"config.offsets.race_sex.{raceSex}")}"))
				this.SelectedRaceSexId = raceSex;
		}

		ImGui.SameLine(0, spacing);
		ImGui.Text($"Selected Bone: {boneDisplay}");
	}

	private void DrawSkeletonCombo() {
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
		using (var _combo = ImRaii.Combo("##RaceSexChooser", this.SelectedRaceSexId, ImGuiComboFlags.NoPreview))
			if (_combo.Success)
				foreach (var raceSex in this.Config.BoneOffsets.Keys.OrderBy(k => k).ToList())
					if (ImGui.Selectable(this._locale.Translate($"config.offsets.race_sex.{raceSex}"), raceSex == this.SelectedRaceSexId))
						this.SelectedRaceSexId = raceSex;

		ImGui.SameLine(0, spacing);
		ImGui.Text($"Skeleton: {this._locale.Translate($"config.offsets.race_sex.{this.SelectedRaceSexId}")}");

		// todo: remove with v0.3 release
		var buttonPadding = ImGui.GetStyle().FramePadding.X * 2;
		var textSize = ImGui.CalcTextSize("Load Legacy Offsets").X;
		// nudge to the edge with respect to legacy button size + spacing + trash button size
		ImGui.SameLine(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - buttonPadding - textSize - spacing - Buttons.CalcSize());
		using (ImRaii.Disabled(!ImGui.IsKeyDown(ImGuiKey.ModShift) || !ImGui.IsKeyDown(ImGuiKey.ModCtrl))) {
			if (ImGui.Button("Load Legacy Offsets"))
				this.Config.LoadLegacyFromClipboard(this.SelectedRaceSexId);

			if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
				using var _ = ImRaii.Tooltip();
				ImGui.Text($"Warning: This will replace ALL current offsets for {
					this._locale.Translate($"config.offsets.race_sex.{this.SelectedRaceSexId}")
				}.\nThis function is only usable with a valid set of v0.2 offsets and will be deprecated with v0.3's release.\nHold CTRL+Shift to confirm.");
			}
		}

		ImGui.SameLine(0, spacing);
		using (ImRaii.Disabled(!ImGui.IsKeyDown(ImGuiKey.ModShift)))
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.TrashAlt, $"Shift+click to clear all offsets for {this._locale.Translate($"config.offsets.race_sex.{this.SelectedRaceSexId}")}"))
				this.Config.RemoveOffsetsForId(this.SelectedRaceSexId!);
	}

	private void DrawBoneOffsets() {
		// buttons | X | Y | Z | bonename
		var oldPadding = ImGui.GetStyle().CellPadding;
		using var tablePad = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(2, 2));
		using var _table = ImRaii.Table("##BoneOffsetTable", 5, ImGuiTableFlags.Borders);
		if (!_table.Success) return;

		ImGui.TableSetupColumn("##BoneButtons", ImGuiTableColumnFlags.WidthFixed);
		ImGui.TableSetupColumn("X");
		ImGui.TableSetupColumn("Y");
		ImGui.TableSetupColumn("Z");
		ImGui.TableSetupColumn("Bone Name");
		ImGui.TableHeadersRow();

		foreach (var (bone, vec) in this.Config.BoneOffsets[this.SelectedRaceSexId!].OrderBy(k => k.Key).ToList()) {
			var vector = vec;
			if (this.DrawOffsetRow(bone, ref vector, oldPadding))
				this.Config.UpsertOffset(this.SelectedRaceSexId!, bone, vector);
		}
	}

	private bool DrawOffsetRow(string bone, ref Vector3 vec, Vector2 padding) {
		var result = false;
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;

		ImGui.TableNextRow();
		using var _id = ImRaii.PushId($"##{bone}OffsetRow");

		// buttons - wrap with old padding to handle the nested raii padding push
		using (ImRaii.PushStyle(ImGuiStyleVar.CellPadding, padding)) {
			ImGui.TableNextColumn();
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.Copy, "Copy offset values"))
				ImGui.SetClipboardText(Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(vec))));
			ImGui.SameLine(0, spacing);

			if (Buttons.IconButtonTooltip(FontAwesomeIcon.Paste, "Paste offset values"))
				if (this.LoadClipboardVector(ref vec))
					result = true;
			ImGui.SameLine(0, spacing);

			using (ImRaii.Disabled(!ImGui.IsKeyDown(ImGuiKey.ModShift)))
				if (Buttons.IconButtonTooltip(FontAwesomeIcon.Trash, "Shift+click to delete bone offset")) {
					this.Config.RemoveOffset(this.SelectedRaceSexId!, bone);
					return false;
				}
		}

		// todo: centering vertically
		// X
		ImGui.TableNextColumn();
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		result |= ImGui.DragFloat("##X", ref vec.X, 0.001f, 0, 0, "%.3f", ImGuiSliderFlags.NoRoundToFormat);

		// Y
		ImGui.TableNextColumn();
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		result |= ImGui.DragFloat("##Y", ref vec.Y, 0.001f, 0, 0, "%.3f", ImGuiSliderFlags.NoRoundToFormat);

		// Z
		ImGui.TableNextColumn();
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		result |= ImGui.DragFloat("##Z", ref vec.Z, 0.001f, 0, 0, "%.3f", ImGuiSliderFlags.NoRoundToFormat);

		// BoneName (FriendlyName)
		ImGui.TableNextColumn();
		string friendlyName = bone;
		if (this._locale.HasTranslationFor($"bone.{bone}"))
			friendlyName += $" ({this._locale.Translate($"bone.{bone}")})";
		ImGui.Text(friendlyName);

		return result;
	}

	private bool LoadClipboardVector(ref Vector3 vec) {
		try {
			vec = JsonConvert.DeserializeObject<Vector3>(Encoding.UTF8.GetString(Convert.FromBase64String(ImGui.GetClipboardText())));
			return true;
		} catch (Exception e) {
			Ktisis.Log.Error($"Could not deserialize clipboard vector: {e}");
			return false;
		}
	}
}
