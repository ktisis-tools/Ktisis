using System.IO;

using ImGuiNET;

using Dalamud.Interface;
using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Util;
using Ktisis.Overlay;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Poses;
using Ktisis.Data.Files;
using Ktisis.Data.Serialization;
using Ktisis.Interface.Components;

namespace Ktisis.Interface.Windows.Workspace.Tabs {
	public static class PoseTab {
		public static TransformTable Transform = new();
		
		public static PoseContainer _TempPose = new();
		
		public unsafe static void Draw(GameObject target) {
			var cfg = Ktisis.Configuration;

			if (target == null) return;

			var actor = (Actor*)target.Address;
			if (actor->Model == null) return;

			// Extra Controls
			ControlButtons.DrawExtra();

			// Parenting

			var parent = cfg.EnableParenting;
			if (ImGui.Checkbox("Parenting", ref parent))
				cfg.EnableParenting = parent;

			// Transform table
			TransformTable(actor);

			ImGui.Spacing();

			// Bone categories
			if (ImGui.CollapsingHeader("Bone Categories")) {

				if (!Categories.DrawToggleList(cfg)) {
					ImGui.Text("No bone found.");
					ImGui.Text("Show Skeleton (");
					ImGui.SameLine();
					GuiHelpers.Icon(FontAwesomeIcon.EyeSlash);
					ImGui.SameLine();
					ImGui.Text(") to fill this.");
				}
			}

			// Bone tree
			BoneTree.Draw(actor);

			// Import & Export
			if (ImGui.CollapsingHeader("Import & Export"))
				ImportExportPose(actor);

			// Advanced
			if (ImGui.CollapsingHeader("Advanced (Debug)")) {
				DrawAdvancedDebugOptions(actor);
			}

			ImGui.EndTabItem();
		}
		
		public static unsafe void DrawAdvancedDebugOptions(Actor* actor) {
			if(ImGui.Button("Reset Current Pose") && actor->Model != null)
				actor->Model->SyncModelSpace();

			if(ImGui.Button("Set to Reference Pose") && actor->Model != null)
				actor->Model->SyncModelSpace(true);

			if(ImGui.Button("Store Pose") && actor->Model != null)
				_TempPose.Store(actor->Model->Skeleton);
			ImGui.SameLine();
			if(ImGui.Button("Apply Pose") && actor->Model != null)
				_TempPose.Apply(actor->Model->Skeleton);

			if(ImGui.Button("Force Redraw"))
				actor->Redraw();
		}
		
		// Transform Table actor and bone names display, actor related extra

		private static unsafe bool TransformTable(Actor* target) {
			var select = Skeleton.BoneSelect;
			var bone = Skeleton.GetSelectedBone();

			if (!select.Active) return Transform.Draw(target);
			if (bone == null) return false;

			return Transform.Draw(bone);
		}
		
		public unsafe static void ImportExportPose(Actor* actor) {
			ImGui.Spacing();
			ImGui.Text("Transforms");

			// Transforms

			var trans = Ktisis.Configuration.PoseTransforms;

			var rot = trans.HasFlag(PoseTransforms.Rotation);
			if (ImGui.Checkbox("Rotation##ImportExportPose", ref rot))
				trans = trans.ToggleFlag(PoseTransforms.Rotation);

			var pos = trans.HasFlag(PoseTransforms.Position);
			var col = pos;
			ImGui.SameLine();
			if (col) ImGui.PushStyleColor(ImGuiCol.Text, 0xff00fbff);
			if (ImGui.Checkbox("Position##ImportExportPose", ref pos))
				trans = trans.ToggleFlag(PoseTransforms.Position);
			if (col) ImGui.PopStyleColor();

			var scale = trans.HasFlag(PoseTransforms.Scale);
			col = scale;
			ImGui.SameLine();
			if (col) ImGui.PushStyleColor(ImGuiCol.Text, 0xff00fbff);
			if (ImGui.Checkbox("Scale##ImportExportPose", ref scale))
				trans = trans.ToggleFlag(PoseTransforms.Scale);
			if (col) ImGui.PopStyleColor();

			if (trans > PoseTransforms.Rotation) {
				ImGui.TextColored(
					Workspace.ColYellow,
					"* Importing may have unexpected results."
				);
			}

			Ktisis.Configuration.PoseTransforms = trans;

			ImGui.Spacing();
			ImGui.Text("Modes");

			// Modes

			var modes = Ktisis.Configuration.PoseMode;

			var body = modes.HasFlag(PoseMode.Body);
			if (ImGui.Checkbox("Body##ImportExportPose", ref body))
				modes = modes.ToggleFlag(PoseMode.Body);

			var face = modes.HasFlag(PoseMode.Face);
			ImGui.SameLine();
			if (ImGui.Checkbox("Expression##ImportExportPose", ref face))
				modes = modes.ToggleFlag(PoseMode.Face);

			Ktisis.Configuration.PoseMode = modes;

			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Spacing();

			var isUseless = trans == 0 || modes == 0;

			if (isUseless) ImGui.BeginDisabled();
			if (ImGui.Button("Import##ImportExportPose")) {
				KtisisGui.FileDialogManager.OpenFileDialog(
					"Importing Pose",
					"Pose Files (.pose){.pose}",
					(success, path) => {
						if (!success) return;

						var content = File.ReadAllText(path[0]);
						var pose = JsonParser.Deserialize<PoseFile>(content);
						if (pose == null) return;

						if (actor->Model == null) return;

						var skeleton = actor->Model->Skeleton;
						if (skeleton == null) return;

						pose.ConvertLegacyBones();

						if (pose.Bones != null) {
							for (var p = 0; p < skeleton->PartialSkeletonCount; p++) {
								switch (p) {
									case 0:
										if (!body) continue;
										break;
									case 1:
										if (!face) continue;
										break;
								}

								pose.Bones.ApplyToPartial(skeleton, p, trans);
							}
						}
					},
					1,
					null
				);
			}
			if (isUseless) ImGui.EndDisabled();
			ImGui.SameLine();
			if (ImGui.Button("Export##ImportExportPose")) {
				KtisisGui.FileDialogManager.SaveFileDialog(
					"Exporting Pose",
					"Pose Files (.pose){.pose}",
					"Untitled.pose",
					".pose",
					(success, path) => {
						if (!success) return;

						var model = actor->Model;
						if (model == null) return;

						var skeleton = model->Skeleton;
						if (skeleton == null) return;

						var pose = new PoseFile();

						pose.Position = model->Position;
						pose.Rotation = model->Rotation;
						pose.Scale = model->Scale;

						pose.Bones = new PoseContainer();
						pose.Bones.Store(skeleton);

						var json = JsonParser.Serialize(pose);
						using (var file = new StreamWriter(path))
							file.Write(json);
					}
				);
			}

			ImGui.Spacing();
		}
	}
}