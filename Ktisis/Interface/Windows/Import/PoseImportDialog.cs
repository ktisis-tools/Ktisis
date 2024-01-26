using System.Linq;

using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

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
		select.OpenDialog = this.OnFileDialogOpen;
	}
	
	private void OnFileDialogOpen(FileSelect<PoseFile> sender) {
		this._ctx.Interface.OpenPoseFile(sender.SetFile);
	}

	// Draw UI

	public override void Draw() {
		ImGui.Text($"Importing pose for {this.Target.Name}");
		ImGui.Spacing();
		
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

		var trans = this._ctx.Config.File.ImportPoseTransforms;

		var rotation = trans.HasFlag(PoseTransforms.Rotation);
		if (ImGui.Checkbox("Rotation##PoseImportRot", ref rotation))
			this._ctx.Config.File.ImportPoseTransforms ^= PoseTransforms.Rotation;
		
		ImGui.SameLine();

		var position = trans.HasFlag(PoseTransforms.Position);
		if (ImGui.Checkbox("Position##PoseImportPos", ref position))
			this._ctx.Config.File.ImportPoseTransforms ^= PoseTransforms.Position;
		
		ImGui.SameLine();

		var scale = trans.HasFlag(PoseTransforms.Scale);
		if (ImGui.Checkbox("Scale##PoseImportScale", ref scale))
			this._ctx.Config.File.ImportPoseTransforms ^= PoseTransforms.Scale;
	}

	private void DrawApplyModes(bool isSelectBones) {
		using var _ = ImRaii.Disabled(!isSelectBones);
		ImGui.Checkbox("Apply selected bones", ref this._ctx.Config.File.ImportPoseSelectedBones);
	}
	
	// Apply pose

	private void ApplyPoseFile(bool isSelectBones) {
		var file = this._select.Selected?.File;
		if (file == null) return;

		var pose = this.Target.Pose;
		if (pose == null) return;

		var transforms = this._ctx.Config.File.ImportPoseTransforms;
		var selectedBones = isSelectBones && this._ctx.Config.File.ImportPoseSelectedBones;
		this._ctx.Posing.ApplyPoseFile(pose, file, transforms, selectedBones);
	}
}
