using System.IO;
using System.Numerics;

using ImGuiNET;

using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Util;
using Ktisis.Overlay;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Poses;
using Ktisis.Localization;
using Ktisis.Interop.Hooks;
using Ktisis.Interface.Components;
using Ktisis.Interface.Windows.ActorEdit;
using Ktisis.Data.Files;
using Ktisis.Data.Serialization;
using Ktisis.Interface.Windows.Toolbar;

using static Ktisis.Data.Files.AnamCharaFile;

namespace Ktisis.Interface.Windows.Workspace
{
    public static class Workspace {
		public static bool Visible = false;
		
		

		public static Vector4 ColGreen = new Vector4(0, 255, 0, 255);
		public static Vector4 ColYellow = new Vector4(255, 250, 0, 255);
		public static Vector4 ColRed = new Vector4(255, 0, 0, 255);

		public static TransformTable Transform = new();

		public static FileDialogManager FileDialogManager = new FileDialogManager();

		// Toggle visibility

		public static void Show() => Visible = true;
		public static void Toggle() => Visible = !Visible;
		
		public static void OnEnterGposeToggle(Structs.Actor.State.ActorGposeState gposeState) {
			if (Ktisis.Configuration.OpenKtisisMethod == OpenKtisisMethod.OnEnterGpose)
				Visible = gposeState == Structs.Actor.State.ActorGposeState.ON;
		}

		public static float PanelHeight => ImGui.GetTextLineHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().FramePadding.Y;

		// Draw window

		public static void Draw() {
			if (!Visible)
				return;

			var gposeOn = Ktisis.IsInGPose;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			if (ImGui.Begin($"Ktisis ({Ktisis.Version})", ref Visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)) {

				ControlButtons.PlaceAndRenderSettings();

				ImGui.BeginGroup();
				ImGui.AlignTextToFramePadding();

				ImGui.TextColored(
					gposeOn ? ColGreen : ColRed,
					gposeOn ? "GPose Enabled" : "GPose Disabled"
				);

				if (PoseHooks.AnamPosingEnabled) {
					ImGui.TextColored(
						ColYellow,
						"Anamnesis Enabled"	
					);
				}

				ImGui.EndGroup();

				ImGui.SameLine();

				// Pose switch
				ControlButtons.DrawPoseSwitch();

				var target = Ktisis.GPoseTarget;
				if (target == null) return;

				// Selection info
				ImGui.Spacing();
				SelectInfo(target);

				// Actor control

				ImGui.Spacing();
				ImGui.Separator();

				if (ImGui.BeginTabBar(Locale.GetString("Workspace"))) {
					if (ImGui.BeginTabItem(Locale.GetString("Actor")))
						ActorTab(target);
					/*if (ImGui.BeginTabItem(Locale.GetString("Scene")))
						SceneTab();*/
					if (ImGui.BeginTabItem(Locale.GetString("Pose")))
						PoseTab(target);
				}
			}

			ImGui.PopStyleVar();
			ImGui.End();
		}

		// Actor tab (Real)

		private unsafe static void ActorTab(GameObject target) {
			var cfg = Ktisis.Configuration;

			if (target == null) return;

			var actor = (Actor*)target.Address;
			if (actor->Model == null) return;

			// Actor details

			ImGui.Spacing();

			// Customize button
			if (ImGuiComponents.IconButton(FontAwesomeIcon.UserEdit)) {
				if (EditActor.Visible)
					EditActor.Hide();
				else
					EditActor.Show();
			}
			ImGui.SameLine();
			ImGui.Text("Edit actor's appearance");

			ImGui.Spacing();

			// Actor list
			ActorsList.Draw();

			// Animation control
			AnimationControls.Draw(target);

			// Gaze control
			if (ImGui.CollapsingHeader("Gaze Control")) {
				if (PoseHooks.PosingEnabled)
					ImGui.TextWrapped("Gaze controls are unavailable while posing.");
				else
					EditGaze.Draw(actor);
			}
			
			// Status Effect control
			StatusEffectControls.Draw(actor);

			// Import & Export
			if (ImGui.CollapsingHeader("Import & Export"))
				ImportExportChara(actor);

			ImGui.EndTabItem();
		}

		// Pose tab

		public static PoseContainer _TempPose = new();

		private unsafe static void PoseTab(GameObject target) {
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

		// Selection details

		private unsafe static void SelectInfo(GameObject target) {
			var actor = (Actor*)target.Address;

			var select = Skeleton.BoneSelect;
			var bone = Skeleton.GetSelectedBone();

			var frameSize = new Vector2(ImGui.GetContentRegionAvail().X - GuiHelpers.WidthMargin(), PanelHeight);
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(ImGui.GetStyle().FramePadding.X, ImGui.GetStyle().FramePadding.Y / 2));
			if (ImGui.BeginChildFrame(8, frameSize, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar)) {
				GameAnimationIndicator();

				ImGui.BeginGroup();

				// display target name
				ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (ImGui.GetStyle().FramePadding.Y / 2));
				ImGui.Text(actor->GetNameOrId());

				// display selected bone name
				ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (ImGui.GetStyle().ItemSpacing.Y / 2) - (ImGui.GetStyle().FramePadding.Y / 2));
				if (select.Active && bone != null) {
					ImGui.Text($"{bone.LocaleName}");
				} else {
					ImGui.BeginDisabled();
					ImGui.Text("No bone selected");
					ImGui.EndDisabled();
				}

				ImGui.EndGroup();

				ImGui.EndChildFrame();
			}
			ImGui.PopStyleVar();
		}

		private static void GameAnimationIndicator() {
			var target = Ktisis.GPoseTarget;
			if (target == null) return;

			var isGamePlaybackRunning = PoseHooks.IsGamePlaybackRunning(target);
			var icon = isGamePlaybackRunning ? FontAwesomeIcon.Play : FontAwesomeIcon.Pause;

			var size = GuiHelpers.CalcIconSize(icon).X;

			ImGui.SameLine(size / 1.5f);

			ImGui.BeginGroup();

			ImGui.Dummy(new Vector2(size, size) / 2);

			GuiHelpers.Icon(icon);
			GuiHelpers.Tooltip(isGamePlaybackRunning ? "Game Animation is playing for this target." + (PoseHooks.PosingEnabled ? "\nPosing may reset periodically." : "") : "Game Animation is paused for this target." + (!PoseHooks.PosingEnabled ? "\nAnimation Control Can be used." : ""));

			ImGui.EndGroup();

			ImGui.SameLine(size * 2.5f);
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
					ColYellow,
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

		public unsafe static void ImportExportChara(Actor* actor) {
			var mode = Ktisis.Configuration.CharaMode;

			// Equipment

			ImGui.BeginGroup();
			ImGui.Text("Equipment");

			var gear = mode.HasFlag(SaveModes.EquipmentGear);
			if (ImGui.Checkbox("Gear##ImportExportChara", ref gear))
				mode ^= SaveModes.EquipmentGear;

			var accs = mode.HasFlag(SaveModes.EquipmentAccessories);
			if (ImGui.Checkbox("Accessories##ImportExportChara", ref accs))
				mode ^= SaveModes.EquipmentAccessories;

			var weps = mode.HasFlag(SaveModes.EquipmentWeapons);
			if (ImGui.Checkbox("Weapons##ImportExportChara", ref weps))
				mode ^= SaveModes.EquipmentWeapons;

			ImGui.EndGroup();

			// Appearance

			ImGui.SameLine();
			ImGui.BeginGroup();
			ImGui.Text("Appearance");

			var body = mode.HasFlag(SaveModes.AppearanceBody);
			if (ImGui.Checkbox("Body##ImportExportChara", ref body))
				mode ^= SaveModes.AppearanceBody;

			var face = mode.HasFlag(SaveModes.AppearanceFace);
			if (ImGui.Checkbox("Face##ImportExportChara", ref face))
				mode ^= SaveModes.AppearanceFace;

			var hair = mode.HasFlag(SaveModes.AppearanceHair);
			if (ImGui.Checkbox("Hair##ImportExportChara", ref hair))
				mode ^= SaveModes.AppearanceHair;

			ImGui.EndGroup();

			// Import & Export buttons

			Ktisis.Configuration.CharaMode = mode;

			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Spacing();

			var isUseless = mode == SaveModes.None;
			if (isUseless) ImGui.BeginDisabled();

			if (ImGui.Button("Import##ImportExportChara")) {
				KtisisGui.FileDialogManager.OpenFileDialog(
					"Importing Character",
					"Anamnesis Chara (.chara){.chara}",
					(success, path) => {
						if (!success) return;

						var content = File.ReadAllText(path[0]);
						var chara = JsonParser.Deserialize<AnamCharaFile>(content);
						if (chara == null) return;

						chara.Apply(actor, mode);
					},
					1,
					null
				);
			}

			ImGui.SameLine();

			if (ImGui.Button("Export##ImportExportChara")) {
				KtisisGui.FileDialogManager.SaveFileDialog(
					"Exporting Character",
					"Anamnesis Chara (.chara){.chara}",
					"Untitled.chara",
					".chara",
					(success, path) => {
						if (!success) return;

						var chara = new AnamCharaFile();
						chara.WriteToFile(*actor, mode);

						var json = JsonParser.Serialize(chara);
						using (var file = new StreamWriter(path))
							file.Write(json);
					}
				);
			}

			if (isUseless) ImGui.EndDisabled();

			ImGui.Spacing();
		}
	}
}
