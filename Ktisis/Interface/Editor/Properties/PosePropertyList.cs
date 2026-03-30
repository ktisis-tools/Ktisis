using System.Diagnostics.CodeAnalysis;

using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

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
		if (partialIndex == -1) return "Reset All Skeletons";
		var name = pose.GetPartialInfo(partialIndex)?.Name ?? "N/A";
		return partialIndex switch {
			0 => "Reset Body",
			1 => "Reset Face",
			2 => "Reset Hair",
			_ => $"Reset Skeleton #{partialIndex} ({name})"
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
		
		builder.AddHeader("Pose", () => this.DrawPoseTab(pose), priority: 1);
	}

	private async void DrawPoseTab(EntityPose pose) {
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
		
		// Parenting toggle
		ImGui.Checkbox(this._locale.Translate("transform_edit.transforms.parenting"), ref this._ctx.Config.Gizmo.ParentBones);
		
		// Import/export when ActorEntity is being drawn for
		var actor = pose.Parent;
		if (actor is not ActorEntity) return;
		ImGui.Spacing();

		if (ImGui.Button("Export Pose"))
			await this._ctx.Interface.OpenPoseExport(pose);
		ImGui.SameLine(0, spacing);

		if (ImGui.Button("Flip Pose"))
			await this._ctx.Posing.ApplyFlipPose(pose);
		ImGui.Spacing();

		if (ImGui.Button("Stash Pose"))
			await this._ctx.Posing.StashPose(pose);
		ImGui.SameLine(0, spacing);

		// todo: GLib.ButtonTooltip? currently only have a helper for IconButtonTooltip
		var _hint = "";
		using (var _disabled = ImRaii.Disabled(this._ctx.Posing.StashedPose == null)) {
			_hint = _disabled ? "" : $"Pose stashed at {this._ctx.Posing.StashedAt} from Actor {this._ctx.Posing.StashedFrom}";
			if (ImGui.Button("Apply Pose"))
				await this._ctx.Posing.ApplyStashedPose(pose);
		}
		if (ImGui.IsItemHovered()) {
			using (ImRaii.Tooltip())
				ImGui.Text(_hint);
		}

		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();
		ImGui.Text("Reference pose...");
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
		if (ImGui.Button("Reset Pose")) {
			if (this._partialIndex == -1)
				await this._ctx.Posing.ApplyReferencePose(pose);
			else
				await this._ctx.Posing.ApplyPartialReferencePose(pose, this._partialIndex);
		}

		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();
		ImGui.Text($"Import pose file...");
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
