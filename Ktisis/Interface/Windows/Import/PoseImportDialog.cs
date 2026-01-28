using System.Linq;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using Ktisis.Data.Config;
using Ktisis.Data.Files;
using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Data;
using Ktisis.Interface.Components.Files;
using Ktisis.Interface.Types;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Interface.Windows.Import;

public class PoseImportDialog : EntityEditWindow<ActorEntity> {
	private readonly IEditorContext _ctx;

	private readonly FileSelect<PoseFile> _select;

	public PoseImportDialog(
		IEditorContext ctx,
		FileSelect<PoseFile> select
	) : base(
		"Import Pose",
		ctx,
		ImGuiWindowFlags.AlwaysAutoResize
	) {
		this._ctx = ctx;
		this._select = select;
		select.OnOpenDialog = this.OnFileDialogOpen;
	}
	
	private void OnFileDialogOpen(FileSelect<PoseFile> sender) {
		this._ctx.Interface.OpenPoseFile(sender.SetFile);
	}

	// Draw UI

	public override void Draw() {
		this.UpdateTarget();
		
		ImGui.Text($"Importing pose for {this.Target.Name}");
		ImGui.Spacing();

		this.DrawEmbed();
	}

	public void DrawEmbed() {
		this.PreDraw();
		this._select.Draw();
		
		ImGui.Spacing();
		this.DrawPoseApplication();
		ImGui.Spacing();
	}
	
	// Pose application

	private void DrawPoseApplication() {
		using var _ = ImRaii.Disabled(!this._select.IsFileOpened);
		
		var isSelectBones = this.Target.Recurse()
			.Where(child => child is SkeletonNode)
			.Any(child => child.IsSelected);
		
		this.DrawTransformSelect();
		ImGui.Spacing();
		this.DrawApplyModes(isSelectBones);
		ImGui.Spacing();
		ImGui.Spacing();

		if (ImGui.Button("Apply"))
			this.ApplyPoseFile(isSelectBones);
	}

	private void DrawTransformSelect() {
		ImGui.Text("Transforms:");

		var file = this._ctx.Config.File;
		var trans = file.ImportPoseTransforms;

		var rotation = trans.HasFlag(PoseTransforms.Rotation);
		if (ImGui.Checkbox("Rotation##PoseImportRot", ref rotation))
			file.ImportPoseTransforms ^= PoseTransforms.Rotation;
		
		ImGui.SameLine();

		// if operating on a .cmp file, force disable position and scale as they're always dummy
		var isCmp = this._select.Selected != null && this._select.Selected.Path.EndsWith(".cmp");

		var position = trans.HasFlag(PoseTransforms.Position);
		var scale = trans.HasFlag(PoseTransforms.Scale);
		using var _ = ImRaii.Disabled(isCmp);
		if (isCmp) {
			position = false;
			file.ImportPoseTransforms &= ~PoseTransforms.Position;
			scale = false;
			file.ImportPoseTransforms &= ~PoseTransforms.Scale;
		}

		if (ImGui.Checkbox("Position##PoseImportPos", ref position))
			file.ImportPoseTransforms ^= PoseTransforms.Position;
		
		ImGui.SameLine();

		if (ImGui.Checkbox("Scale##PoseImportScale", ref scale))
			file.ImportPoseTransforms ^= PoseTransforms.Scale;
	}

	private void DrawApplyModes(bool isSelectBones) {
		ImGui.Text("Modes:");

		var file = this._ctx.Config.File;
		var modes = file.ImportPoseModes;

		var isSelectiveImport = file.ImportPoseSelectedBones && isSelectBones;
		using (ImRaii.Disabled(!isSelectBones)) {
			if (ImGui.Checkbox("Apply selected bones", ref isSelectiveImport))
				file.ImportPoseSelectedBones ^= true;
		}

		if (isSelectiveImport) {
			using (ImRaii.PushIndent()) {
				ImGui.Checkbox("Include descendants", ref file.SelectedBonesIncludeDescendants);

				var hasPosition = file.ImportPoseTransforms.HasFlag(PoseTransforms.Position);
				using (ImRaii.Disabled(!hasPosition))
					ImGui.Checkbox("Anchor group positions", ref file.AnchorPoseSelectedBones);
			}
		}

		if (!isSelectiveImport || file.SelectedBonesIncludeDescendants) {
			var body = modes.HasFlag(PoseMode.Body);
			if (ImGui.Checkbox("Body##PoseImportBody", ref body))
				file.ImportPoseModes ^= PoseMode.Body;

			ImGui.SameLine();

			var face = modes.HasFlag(PoseMode.Face);
			if (ImGui.Checkbox("Face##PoseImportFace", ref face))
				file.ImportPoseModes ^= PoseMode.Face;
			if (face && this._select.IsFileOpened && this.Target.Pose?.HasDTFace() != _select.Selected?.File.HasDTFace()) {
				ImGui.SameLine();
				Icons.DrawIcon(FontAwesomeIcon.ExclamationTriangle, ColorHelpers.RgbaVector4ToUint(ImGuiColors.DalamudYellow));
				if (ImGui.IsItemHovered())
					ImGui.SetTooltip("Face will not be imported from the selected pose file due to incompatibility with the selected actor.");
			}
		}

		ImGui.Checkbox("Exclude ear bones", ref file.ExcludePoseEarBones);
	}
	
	// Apply pose

	private void ApplyPoseFile(bool isSelectBones) {
		var file = this._select.Selected?.File;
		if (file == null) return;

		var pose = this.Target.Pose;
		if (pose == null) return;

		var cfg = this._ctx.Config.File;
		var selectedBones = isSelectBones && cfg.ImportPoseSelectedBones;
		var includeDescendants = cfg.SelectedBonesIncludeDescendants;
		var anchorGroups = cfg.AnchorPoseSelectedBones;
		var excludeEars = cfg.ExcludePoseEarBones;
		this._ctx.Posing.ApplyPoseFile(pose, file, cfg.ImportPoseModes, cfg.ImportPoseTransforms, selectedBones, includeDescendants, anchorGroups, excludeEars);
	}
}
