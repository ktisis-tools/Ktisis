using System.Linq;

using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;

using ImGuiNET;

using Ktisis.Data.Config;
using Ktisis.Data.Files;
using Ktisis.Editor;
using Ktisis.Editor.Context;
using Ktisis.Editor.Posing;
using Ktisis.Interface.Components.Files;
using Ktisis.Interface.Types;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Interface.Windows.Pose;

public class PoseImportDialog : EntityEditWindow<ActorEntity> {
	private readonly IFramework _framework;
	private readonly FileDialogManager _dialog;
	private readonly IEditorContext _context;

	private readonly FileSelect<PoseFile> _select;

	private Configuration Config => this._context.Config;

	public PoseImportDialog(
		IFramework framework,
		FileDialogManager dialog,
		IEditorContext context,
		FileSelect<PoseFile> select
	) : base(
		"Import Pose",
		context,
		ImGuiWindowFlags.AlwaysAutoResize
	) {
		this._framework = framework;
		this._dialog = dialog;
		this._context = context;
		this._select = select;
		select.OpenDialog = this.OnFileDialogOpen;
	}
	
	private void OnFileDialogOpen(FileSelect<PoseFile> sender) {
		this._dialog.OpenPoseFile(sender.SetFile);
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
		using var _disable = ImRaii.Disabled(!this._select.IsFileOpened);
		
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

		var trans = this.Config.File.ImportPoseTransforms;

		var rotation = trans.HasFlag(PoseTransforms.Rotation);
		if (ImGui.Checkbox("Rotation##PoseImportRot", ref rotation))
			this.Config.File.ImportPoseTransforms ^= PoseTransforms.Rotation;
		
		ImGui.SameLine();

		var position = trans.HasFlag(PoseTransforms.Position);
		if (ImGui.Checkbox("Position##PoseImportPos", ref position))
			this.Config.File.ImportPoseTransforms ^= PoseTransforms.Position;
		
		ImGui.SameLine();

		var scale = trans.HasFlag(PoseTransforms.Scale);
		if (ImGui.Checkbox("Scale##PoseImportScale", ref scale))
			this.Config.File.ImportPoseTransforms ^= PoseTransforms.Scale;
	}

	private void DrawApplyModes(bool isSelectBones) {
		using var _ = ImRaii.Disabled(!isSelectBones);
		ImGui.Checkbox("Apply selected bones", ref this.Config.File.ImportPoseSelectedBones);
	}
	
	// Apply pose

	private void ApplyPoseFile(bool isSelectBones) {
		var file = this._select.Selected?.File;
		if (file == null) return;

		var pose = (EntityPose?)this.Target.Children.FirstOrDefault(child => child is EntityPose);
		if (pose == null) return;

		var loader = new EntityPoseConverter(pose);
		this._framework.RunOnFrameworkThread(() => {
			if (isSelectBones && this.Config.File.ImportPoseSelectedBones)
				loader.LoadSelectedBones(file.Bones!, PoseTransforms.Rotation);
			else
				loader.Load(file.Bones!, PoseTransforms.Rotation);
		});
	}
}
