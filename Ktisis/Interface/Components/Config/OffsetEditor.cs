using System.Linq;
using System.Numerics;

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
			this.SelectedRaceSexId = this.Config.BoneOffsets.Keys.First();

		// instead set selected race to selection's race if there is one
		if (this.HasContext)
			this.GetRaceSexIdFromSelection();
	}
	private void UpdateContext() {
		this._editorContext = this._ctx.Current;

		// each frame, if we still dont have a race selected but we DO have a context, see if there's a selection to grab race from
		if (this.HasContext && this.SelectedRaceSexId is null)
			this.GetRaceSexIdFromSelection();
	}

	private void GetRaceSexIdFromSelection() {
		if (!this.HasContext) return;
		var target = this._editorContext!.Selection.GetFirstSelected();
		if (
			target switch {
				BoneNode node => node.Pose.Parent,
				BoneNodeGroup group => group.Pose.Parent,
				EntityPose pose => pose.Parent,
				_ => target
			} is ActorEntity actor
		) this.SelectedRaceSexId = actor.GetRaceSexId();
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

		if (ImGui.Button("Export to Clipboard")) {}
		ImGui.SameLine(0, spacing);
		if (ImGui.Button("Import from Clipboard")) {}
		ImGui.SameLine(0, spacing);
		ImGui.Text("You can export and import bone offsets via clipboard here!");
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
				boneDisplay += $" on Skeleton {raceSex}";
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
		)) if (Buttons.IconButtonTooltip(FontAwesomeIcon.Eye, "Open offsets for RaceSex"))
			this.SelectedRaceSexId = raceSex;

		ImGui.SameLine(0, spacing);
		ImGui.Text($"Selected Bone: {boneDisplay}");
	}

	private void DrawSkeletonCombo() {
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
		using (var _combo = ImRaii.Combo("##RaceSexChooser", this.SelectedRaceSexId, ImGuiComboFlags.NoPreview))
			if (_combo.Success)
				foreach (var raceSex in this.Config.BoneOffsets.Keys)
					if (ImGui.Selectable($"race: {raceSex}", raceSex == this.SelectedRaceSexId))
						this.SelectedRaceSexId = raceSex;

		ImGui.SameLine(0, spacing);
		ImGui.Text($"Skeleton: {this.SelectedRaceSexId}");
	}

	private void DrawBoneOffsets() {
		// buttons | X | Y | Z | bonename
		using var tablePad = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, Vector2.Zero);
		using var _table = ImRaii.Table("##BoneOffsetTable", 5, ImGuiTableFlags.Borders);
		if (!_table.Success) return;

		ImGui.TableSetupColumn("##BoneButtons");
		ImGui.TableSetupColumn("X");
		ImGui.TableSetupColumn("Y");
		ImGui.TableSetupColumn("Z");
		ImGui.TableSetupColumn("Bone Name");
		ImGui.TableHeadersRow();

		foreach (var (bone, vec) in this.Config.BoneOffsets[this.SelectedRaceSexId!]) {
			var vector = vec;
			if (this.DrawOffsetRow(bone, ref vector))
				this.Config.UpsertOffset(this.SelectedRaceSexId!, bone, vector);
		}
	}

	private bool DrawOffsetRow(string bone, ref Vector3 vec) {
		var result = false;
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;

		ImGui.TableNextRow();
		using var _id = ImRaii.PushId($"##{bone}OffsetRow");

		// buttons
		ImGui.TableNextColumn();
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Copy, "Copy offset values")) {}
		ImGui.SameLine(0, spacing);
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Paste, "Paste offset values")) {}
		ImGui.SameLine(0, spacing);
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Trash, "Delete bone offset")) {
			this.Config.RemoveOffset(this.SelectedRaceSexId!, bone);
			return result;
		}

		// X
		ImGui.TableNextColumn();
		result |= ImGui.DragFloat("##X", ref vec.X, 0.001f, 0, 0, "%.3f", ImGuiSliderFlags.NoRoundToFormat);

		// Y
		ImGui.TableNextColumn();
		result |= ImGui.DragFloat("##Y", ref vec.Y, 0.001f, 0, 0, "%.3f", ImGuiSliderFlags.NoRoundToFormat);

		// Z
		ImGui.TableNextColumn();
		result |= ImGui.DragFloat("##Z", ref vec.Z, 0.001f, 0, 0, "%.3f", ImGuiSliderFlags.NoRoundToFormat);

		// BoneName
		ImGui.TableNextColumn();
		ImGui.Text(bone);

		return result;
	}
}
