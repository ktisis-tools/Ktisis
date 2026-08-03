using System.Diagnostics.CodeAnalysis;

using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

using GLib.Widgets;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Editor.Properties.Types;
using Ktisis.Interface.Windows.Import;
using Ktisis.Localization;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Interface.Editor.Properties;

public class PosePropertyList : ObjectPropertyList {
	private readonly IEditorContext _ctx;
	private readonly GuiManager _gui;
	private readonly LocaleManager _locale;
	private int _partialIndex;

	private string LabelForPartial(EntityPose pose, int partialIndex) {
		if (partialIndex == -1) return Ktisis.Locale.Translate("object_edit.pose.reference_labels.all");
		var name = pose.GetPartialInfo(partialIndex)?.Name ?? Ktisis.Locale.Translate("object_edit.pose.reference_labels.null");
		return partialIndex switch {
			0 => Ktisis.Locale.Translate("object_edit.pose.reference_labels.body"),
			1 => Ktisis.Locale.Translate("object_edit.pose.reference_labels.face"),
			2 => Ktisis.Locale.Translate("object_edit.pose.reference_labels.hair"),
			_ => $"{Ktisis.Locale.Translate("object_edit.pose.reference_labels.custom")} #{partialIndex} ({name})"
		};
	}

	public PosePropertyList(
		IEditorContext ctx,
		GuiManager gui,
		LocaleManager locale
	) {
		this._gui = gui;
		this._ctx = ctx;
		this._locale = locale;
		this._partialIndex = -1;
	}
	
	public override void Invoke(IPropertyListBuilder builder, SceneEntity entity) {
		if (!TryGetEntityPose(entity, out var pose))
			return;
		
		builder.AddHeader(Ktisis.Locale.Translate("object_edit.pose.headers.pose"), () => this.DrawPoseTab(pose), priority: 1);
	}

	private async void DrawPoseTab(EntityPose pose) {
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
		
		// Import/export when ActorEntity is being drawn for
		var actor = pose.Parent;
		if (actor is not ActorEntity) return;
		ImGui.Spacing();

		if (ImGui.Button(Ktisis.Locale.Translate("object_edit.pose.export")))
			await this._ctx.Interface.OpenPoseExport(pose);
		ImGui.SameLine(0, spacing);

		if (ImGui.Button(Ktisis.Locale.Translate("object_edit.pose.flip")))
			await this._ctx.Posing.ApplyFlipPose(pose);
		ImGui.Spacing();

		if (ImGui.Button(Ktisis.Locale.Translate("object_edit.pose.stash")))
			await this._ctx.Posing.StashPose(pose);
		ImGui.SameLine(0, spacing);

		// todo: GLib.ButtonTooltip? currently only have a helper for IconButtonTooltip
		var _hint = "";
		using (var _disabled = ImRaii.Disabled(this._ctx.Posing.StashedPose == null)) {
			_hint = _disabled.Count > 0 ? "" : $"{Ktisis.Locale.Translate("object_edit.pose.stash.time")} {this._ctx.Posing.StashedAt} {Ktisis.Locale.Translate("object_edit.pose.stash.from")} {this._ctx.Posing.StashedFrom}";
			if (ImGui.Button(Ktisis.Locale.Translate("object_edit.pose.stash.apply")))
				await this._ctx.Posing.ApplyStashedPose(pose);
		}
		if (ImGui.IsItemHovered()) {
			using (ImRaii.Tooltip())
				ImGui.Text(_hint);
		}

		ImGui.Spacing();
		Separators.SeparatorText(Ktisis.Locale.Translate("object_edit.pose.headers.reference"), textColor:ImGui.GetColorU32(ImGuiCol.Header));
		ImGui.Spacing();
		var combo = ImGui.BeginCombo("##PartialSelectList", this.LabelForPartial(pose, this._partialIndex));
		if (combo) {
			// add select-all as the first element
			if (ImGui.Selectable(this.LabelForPartial(pose, -1), this._partialIndex == -1))
				this._partialIndex = -1;

			// add element for each partial index on pose
			foreach (var partial in pose.GetPartialIndices()) {
				var label = this.LabelForPartial(pose, partial);
				label = label.Length <= 60 ? label : label[..60] + "..."; // truncate to ellipsis in case of really long filepaths
				if (ImGui.Selectable(label, this._partialIndex == partial))
					this._partialIndex = partial;
			}
			ImGui.EndCombo();
		}
		ImGui.SameLine(0, spacing);
		if (ImGui.Button(Ktisis.Locale.Translate("object_edit.pose.reference"))) {
			if (this._partialIndex == -1)
				await this._ctx.Posing.ApplyReferencePose(pose);
			else
				await this._ctx.Posing.ApplyPartialReferencePose(pose, this._partialIndex);
		}

		ImGui.Spacing();
		Separators.SeparatorText(Ktisis.Locale.Translate("object_edit.pose.headers.import"), textColor:ImGui.GetColorU32(ImGuiCol.Header));
		ImGui.Spacing();

		// pose import dialog
		var embedEditor = this._gui.GetOrCreate<PoseImportDialog>(this._ctx);
		embedEditor.SetTarget((ActorEntity)actor);
		embedEditor.DrawEmbed();
	}

	// Entity helpers

	private static bool TryGetEntityPose(SceneEntity entity, [NotNullWhen(true)] out EntityPose? result) {
		result = entity switch {
			ActorEntity actor => actor.Pose,
			BoneNodeGroup group => group.Pose,
			BoneNode node => node.Pose,
			EntityPose pose => pose,
			_ => null
		};
		return result != null;
	}
}
