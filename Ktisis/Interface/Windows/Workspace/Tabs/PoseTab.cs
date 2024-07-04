using ImGuiNET;

using Dalamud.Interface;
using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Util;
using Ktisis.Overlay;
using Ktisis.Helpers;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Poses;
using Ktisis.Interface.Components;

namespace Ktisis.Interface.Windows.Workspace.Tabs {
	public static class PoseTab {
		public static TransformTable Transform = new();
		
		public static PoseContainer _TempPose = new();
		
		public unsafe static void Draw(IGameObject target) {
			var cfg = Ktisis.Configuration;

			var actor = (Actor*)target.Address;

			// Extra Controls
			ControlButtons.DrawExtra();

			// Parenting

			var parent = cfg.EnableParenting;
			if (ImGui.Checkbox("Parenting", ref parent))
				cfg.EnableParenting = parent;

			if (actor->Model != null) {
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
			} else {
				ImGui.Text("Target actor has no valid skeleton!");
				ImGui.Spacing();
			}

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
			if (actor->Model != null) {
				if (ImGui.Button("Reset Current Pose"))
					actor->Model->SyncModelSpace();
				if (ImGui.Button("Set to Reference Pose"))
					actor->Model->SyncModelSpace(true);
				if (ImGui.Button("Store Pose"))
					_TempPose.Store(actor->Model->Skeleton);
				ImGui.SameLine();
				if (ImGui.Button("Apply Pose"))
					_TempPose.Apply(actor->Model->Skeleton);
			}

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
			
			var wep = modes.HasFlag(PoseMode.Weapons);
			ImGui.SameLine();
			if (ImGui.Checkbox("Weapons##ImportExportPose", ref wep))
				modes = modes.ToggleFlag(PoseMode.Weapons);

			var posWep = Ktisis.Configuration.PositionWeapons;
			if (modes.HasFlag(PoseMode.Weapons)) {
				ImGui.Spacing();
				if (ImGui.Checkbox("Apply position to weapons##ApplyWepPos", ref posWep))
					Ktisis.Configuration.PositionWeapons = posWep;
			}

			Ktisis.Configuration.PoseMode = modes;

			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Spacing();

			var isUseless = trans == 0 || modes == 0;

			if (isUseless) ImGui.BeginDisabled();
			if (ImGui.Button("Import##ImportExportPose")) {
				KtisisGui.FileDialogManager.OpenFileDialog(
					"Importing Pose",
					"Pose Files{.pose,.cmp}",
					(success, path) => {
						if (!success) return;

						PoseHelpers.ImportPose(actor, path, Ktisis.Configuration.PoseMode);
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

						PoseHelpers.ExportPose(actor, path, Ktisis.Configuration.PoseMode);
					}
				);
			}

			ImGui.Spacing();
		}
	}
}
