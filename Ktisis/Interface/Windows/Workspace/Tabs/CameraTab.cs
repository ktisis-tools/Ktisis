using System.Numerics;

using ImGuiNET;

using Dalamud.Interface;
using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Util;
using Ktisis.Camera;
using Ktisis.Helpers;
using Ktisis.History;
using Ktisis.Structs.Actor;
using Ktisis.Interface.Components;

namespace Ktisis.Interface.Windows.Workspace.Tabs {
	public static class CameraTab {
		private static TransformTable Transform = new(); // this needs a rework.

		public static void Draw() {
			ImGui.Spacing();
			
			DrawTargetLock();
			ImGui.Spacing();
			DrawCameraSelect();
			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Spacing();
			DrawControls();

			ImGui.EndTabItem();
		}

		private static void DrawCameraSelect() {
			var camera = CameraService.GetActiveCamera();
			if (camera == null || !camera.IsValid()) return;
			
			var avail = ImGui.GetContentRegionAvail().X;
			var style = ImGui.GetStyle();

			var plusSize = GuiHelpers.CalcIconSize(FontAwesomeIcon.Plus);
			var camSize = GuiHelpers.CalcIconSize(FontAwesomeIcon.Camera);

			var cameras = CameraService.GetCameraList();

			var isFreecam = camera.WorkCamera != null;
			ImGui.BeginDisabled(isFreecam);
			
			var comboWidth = avail - (style.ItemSpacing.X * 4) + style.ItemInnerSpacing.X - plusSize.X - camSize.X;
			ImGui.SetNextItemWidth(comboWidth);
			if (ImGui.BeginCombo("##CameraSelect", camera.Name)) {
				var id = 0;
				foreach (var cam in cameras) {
					if (!ImGui.Selectable($"{cam.Name}##CameraSelect_{id}")) continue;

					if (cam.IsNative)
						CameraService.Reset();
					else
						CameraService.SetOverride(cam);
				}
				ImGui.EndCombo();
			}
			
			ImGui.SameLine(ImGui.GetCursorPosX() + comboWidth + style.ItemInnerSpacing.X);
			var createNew = GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Plus, "Create new camera");
			if (createNew) {
				Services.Framework.RunOnFrameworkThread(() => {
					var camera = CameraService.SpawnCamera();
					CameraService.SetOverride(camera);

					// Handle this here for cameras explicitly created by the user.
					HistoryItem.CreateCamera(CameraEvent.CreateCamera)
						.SetSubject(camera)
						.AddToHistory();
				});
			}
			
			ImGui.EndDisabled();

			ImGui.SameLine();
			var toggleFree = GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Camera, "Toggle work camera");
			if (toggleFree)
				CameraService.ToggleFreecam();
		}

		private unsafe static void DrawTargetLock() {
			var camera = CameraService.GetActiveCamera();
			if (camera == null || !camera.IsValid()) return;

			var isFreecam = camera.WorkCamera != null;
			
			var tarLock = camera.GetOrbitTarget() is GameObject actor ? (Actor*)actor.Address : null;
			var isTarLocked = tarLock != null || isFreecam;
			
			var target = tarLock != null ? tarLock : Ktisis.Target;
			if (target == null) return;
			
			ImGui.BeginDisabled(isFreecam);
			
			var icon = isTarLocked ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock;
			var tooltip = isTarLocked ? "Unlock orbit target" : "Lock orbit target";
			if (GuiHelpers.IconButtonTooltip(icon, tooltip, default, "CamOrbitLock"))
				camera.SetOrbit(isTarLocked ? null : target->GameObject.ObjectIndex);

			ImGui.SameLine();

			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Sync, "Move camera to target model")) {
				var goPos = target->GameObject.Position;
				if (target->Model != null) {
					var doPos = target->Model->Position;
					var offset = doPos - (Vector3)goPos;
					camera.SetOffset(offset);
				}
			}

			ImGui.SameLine();
			ImGui.BeginDisabled(!isTarLocked);
			ImGui.Text(!isFreecam ? $"Orbiting: {target->GetNameOrId()}" : "Work camera enabled.");
			ImGui.EndDisabled();
			
			ImGui.EndDisabled();
		}

		private unsafe static void DrawControls() {
			var camera = CameraService.GetActiveCamera();
			if (camera == null || !camera.IsValid()) return;

			var gposeCam = camera.AsGPoseCamera();

			// Camera position

			var camEdit = camera.CameraEdit;
			
			var pos = camera.Position;
			var offset = camEdit.Offset ?? Vector3.Zero;

			var posLock = camEdit.Position;
			var isFreecam = camera.WorkCamera != null;
			var isLocked = posLock != null || isFreecam;

			ImGui.BeginDisabled(isFreecam);
			var lockIcon = isLocked ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock;
			var lockTooltip = isLocked ? "Unlock camera position" : "Lock camera position";
			if (GuiHelpers.IconButtonTooltip(lockIcon, lockTooltip, default, "CamPosLock"))
				camera.SetPositionLock(isLocked ? null : pos - offset);
			ImGui.EndDisabled();

			ImGui.SameLine();

			var posCursor = ImGui.GetCursorPosX();
			ImGui.BeginDisabled(!isLocked);
			if (Transform.ColoredDragFloat3("##CamWorldPos", ref pos, 0.005f)) {
				if (camera.WorkCamera is WorkCamera freecam) {
					freecam.Position = pos;
					freecam.InterpPos = pos;
				} else {
					camera.SetPositionLock(pos - offset);
				}
			}
			ImGui.EndDisabled();
			
			ImGui.BeginDisabled(isFreecam); // Below controls disabled under freecam.
			
			// Camera offset
			
			ImGui.Dummy(default);
			ImGui.SameLine();
			GuiHelpers.IconTooltip(FontAwesomeIcon.Plus, "Offset from base position");
			ImGui.SameLine();
			ImGui.SetCursorPosX(posCursor);
			if (Transform.ColoredDragFloat3("##CamOffset", ref offset, 0.005f))
				camera.SetOffset(offset != Vector3.Zero ? offset : null);

			// Angle

			ImGui.Spacing();
			ImGui.Spacing();
			
			PrepareIconTooltip(FontAwesomeIcon.ArrowsSpin, "Camera orbit angle", posCursor);
			var angle = gposeCam->Angle * MathHelpers.Rad2Deg;
			if (Transform.DragFloat2("CameraAngle", ref angle, Ktisis.Configuration.TransformTableBaseSpeedRot))
				gposeCam->Angle = angle * MathHelpers.Deg2Rad;
			
			// Pan

			ImGui.Spacing();
			
			PrepareIconTooltip(FontAwesomeIcon.ArrowsAlt, "Camera pan", posCursor);
			var pan = gposeCam->Pan * MathHelpers.Rad2Deg;
			if (Transform.DragFloat2("CameraPan", ref pan, Ktisis.Configuration.TransformTableBaseSpeedRot))
				gposeCam->Pan = pan * MathHelpers.Deg2Rad;

			ImGui.EndDisabled(); // Above controls disabled under freecam.
			
			// other shit

			ImGui.Spacing();
			ImGui.Spacing();

			ImGui.BeginDisabled(isFreecam);
			PrepareIconTooltip(FontAwesomeIcon.CameraRotate, "Camera rotation", posCursor);
			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
			ImGui.SliderAngle("##CamRotato", ref gposeCam->Rotation, -180, 180, "%.3f", ImGuiSliderFlags.AlwaysClamp);
			ImGui.EndDisabled();

			PrepareIconTooltip(FontAwesomeIcon.VectorSquare, "Field of View", posCursor);
			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
			ImGui.SliderAngle("##CamFoV", ref gposeCam->FoV, -40, 100, "%.3f", ImGuiSliderFlags.AlwaysClamp);

			ImGui.BeginDisabled(isLocked);
			PrepareIconTooltip(FontAwesomeIcon.Moon, "Camera distance", posCursor);
			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
			ImGui.SliderFloat("##CamDist", ref gposeCam->Distance, 0, gposeCam->DistanceMax);
			ImGui.EndDisabled();
		}

		private static void PrepareIconTooltip(FontAwesomeIcon icon, string tooltip, float posCursor) {
			ImGui.Dummy(default);
			ImGui.SameLine();
			GuiHelpers.IconTooltip(icon, tooltip);
			ImGui.SameLine();
			ImGui.SetCursorPosX(posCursor);
		}
	}
}