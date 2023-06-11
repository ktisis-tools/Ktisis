using System.Numerics;

using ImGuiNET;

using Dalamud.Interface;
using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Util;
using Ktisis.Camera;
using Ktisis.Helpers;
using Ktisis.History;
using Ktisis.Structs.Actor;
using Ktisis.Structs.FFXIV;
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

		private unsafe static void DrawCameraSelect() {
			var avail = ImGui.GetContentRegionAvail().X;
			var style = ImGui.GetStyle();

			var plusSize = GuiHelpers.CalcIconSize(FontAwesomeIcon.Plus);
			var camSize = GuiHelpers.CalcIconSize(FontAwesomeIcon.Camera);

			var active = Services.Camera->GetActiveCamera();
			
			var cameras = CameraService.GetCameraList();

			var freeActive = CameraService.Freecam.Active;
			ImGui.BeginDisabled(freeActive);
			
			var label = cameras.TryGetValue((nint)active, out var item) ? item : $"Unknown: 0x{(nint)active:X}";
			var comboWidth = avail - (style.ItemSpacing.X * 4) + style.ItemInnerSpacing.X - plusSize.X - camSize.X;
			ImGui.SetNextItemWidth(comboWidth);
			if (ImGui.BeginCombo("##Camera", label)) {
				var id = 0;
				foreach (var pair in cameras) {
					if (!ImGui.Selectable($"{pair.Value}##Camera{id++}")) continue;

					var ptr = pair.Key;
					if (ptr == (nint)Services.Camera->Camera)
						CameraService.Reset();
					else
						CameraService.SetOverride(pair.Key);
				}
				ImGui.EndCombo();
			}

			ImGui.SameLine(ImGui.GetCursorPosX() + comboWidth + style.ItemInnerSpacing.X);
			var createNew = GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Plus, "Create new camera");
			if (createNew) {
				Services.Framework.RunOnFrameworkThread(() => {
					var camera = CameraService.SpawnCamera();
					CameraService.SetOverride(camera.GameCamera);

					HistoryItem.CreateCamera(CameraEvent.CreateCamera)
						.SetProperty(camera.Name)
						.SetStartValue((nint)active)
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
			var active = Services.Camera->GetActiveCamera();
			var addr = (nint)active;

			var freeActive = CameraService.Freecam.Active;
			
			var tarLock = CameraService.GetTargetLock(addr) is GameObject actor ? (Actor*)actor.Address : null;
			var isTarLocked = tarLock != null || freeActive;
			
			var target = tarLock != null ? tarLock : Ktisis.Target;
			if (target == null) return;
			
			ImGui.BeginDisabled(freeActive);
			
			var icon = isTarLocked ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock;
			var tooltip = isTarLocked ? "Unlock orbit target" : "Lock orbit target";
			if (GuiHelpers.IconButtonTooltip(icon, tooltip, default, "CamOrbitLock"))
				CameraService.SetTargetLock(addr, isTarLocked ? null : target->GameObject.ObjectIndex);

			ImGui.SameLine();

			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Sync, "Move camera to target model")) {
				var goPos = target->GameObject.Position;
				if (target->Model != null) {
					var doPos = target->Model->Position;
					var offset = doPos - (Vector3)goPos;
					CameraService.SetOffset(addr, offset);
				}
			}

			ImGui.SameLine();
			ImGui.BeginDisabled(!isTarLocked);
			ImGui.Text(!freeActive ? $"Orbiting: {target->GetNameOrId()}" : "Work camera enabled.");
			ImGui.EndDisabled();
			
			ImGui.EndDisabled();
		}

		private unsafe static void DrawControls() {
			var camera = (GPoseCamera*)Services.Camera->GetActiveCamera();
			var addr = (nint)camera;
			
			// Camera position

			var camEdit = CameraService.GetCameraEdit(addr);
			
			var pos = camera->Position;
			var offset = camEdit?.Offset ?? Vector3.Zero;

			var posLock = camEdit != null ? camEdit.Position : null;
			var isLocked = posLock != null || CameraService.Freecam.Active;

			ImGui.BeginDisabled(CameraService.Freecam.Active);
			var lockIcon = isLocked ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock;
			var lockTooltip = isLocked ? "Unlock camera position" : "Lock camera position";
			if (GuiHelpers.IconButtonTooltip(lockIcon, lockTooltip, default, "CamPosLock"))
				CameraService.SetPositionLock(addr, isLocked ? null : pos - offset);
			ImGui.EndDisabled();

			ImGui.SameLine();

			var posCursor = ImGui.GetCursorPosX();
			ImGui.BeginDisabled(!isLocked);
			if (Transform.ColoredDragFloat3("##CamWorldPos", ref pos, 0.005f)) {
				if (CameraService.Freecam.Active) {
					CameraService.Freecam.Position = pos;
					CameraService.Freecam.InterpPos = pos;
				} else {
					CameraService.SetPositionLock(addr, pos - offset);
				}
			}
			ImGui.EndDisabled();
			
			ImGui.BeginDisabled(CameraService.Freecam.Active); // Below controls disabled under freecam.
			
			// Camera offset
			
			ImGui.Dummy(default);
			ImGui.SameLine();
			GuiHelpers.IconTooltip(FontAwesomeIcon.Plus, "Offset from base position");
			ImGui.SameLine();
			ImGui.SetCursorPosX(posCursor);
			if (Transform.ColoredDragFloat3("##CamOffset", ref offset, 0.005f))
				CameraService.SetOffset(addr, offset != Vector3.Zero ? offset : null);

			// Angle

			ImGui.Spacing();
			ImGui.Spacing();
			
			PrepareIconTooltip(FontAwesomeIcon.ArrowsSpin, "Camera orbit angle", posCursor);
			var angle = camera->Angle * MathHelpers.Rad2Deg;
			if (Transform.DragFloat2("CameraAngle", ref angle, Ktisis.Configuration.TransformTableBaseSpeedRot))
				camera->Angle = angle * MathHelpers.Deg2Rad;
			
			// Pan

			ImGui.Spacing();
			
			PrepareIconTooltip(FontAwesomeIcon.ArrowsAlt, "Camera pan", posCursor);
			var pan = camera->Pan * MathHelpers.Rad2Deg;
			if (Transform.DragFloat2("CameraPan", ref pan, Ktisis.Configuration.TransformTableBaseSpeedRot))
				camera->Pan = pan * MathHelpers.Deg2Rad;

			ImGui.EndDisabled(); // Above controls disabled under freecam.
			
			// other shit

			ImGui.Spacing();
			ImGui.Spacing();

			ImGui.BeginDisabled(CameraService.Freecam.Active);
			PrepareIconTooltip(FontAwesomeIcon.CameraRotate, "Camera rotation", posCursor);
			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
			ImGui.SliderAngle("##CamRotato", ref camera->Rotation, -180, 180, "%.3f", ImGuiSliderFlags.AlwaysClamp);
			ImGui.EndDisabled();

			PrepareIconTooltip(FontAwesomeIcon.VectorSquare, "Field of View", posCursor);
			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
			ImGui.SliderAngle("##CamFoV", ref camera->FoV, -40, 100, "%.3f", ImGuiSliderFlags.AlwaysClamp);

			ImGui.BeginDisabled(isLocked);
			PrepareIconTooltip(FontAwesomeIcon.Moon, "Camera distance", posCursor);
			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
			ImGui.SliderFloat("##CamDist", ref camera->Distance, 0, camera->DistanceMax);
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