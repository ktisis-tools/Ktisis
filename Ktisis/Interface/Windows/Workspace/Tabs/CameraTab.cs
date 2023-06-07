using System.Numerics;

using ImGuiNET;

using Dalamud.Interface;
using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Util;
using Ktisis.Camera;
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
			ImGui.BeginDisabled(!isTarLocked);
			ImGui.Text(!freeActive ? $"Orbiting: {target->GetNameOrId()}" : "Work camera enabled.");
			ImGui.EndDisabled();
			
			ImGui.EndDisabled();
		}

		private unsafe static void DrawControls() {
			var camera = (GPoseCamera*)Services.Camera->GetActiveCamera();
			var addr = (nint)camera;

			var camObj = &camera->GameCamera.CameraBase.SceneCamera.Object;

			ImGui.Spacing();

			ImGui.Text("Camera position:");
			
			var camEdit = CameraService.GetCameraEdit(addr);
			
			var pos = (Vector3)camObj->Position;
			var offset = camEdit?.Offset ?? Vector3.Zero;

			var posLock = camEdit != null ? camEdit.Position : null;
			var isLocked = posLock != null;
			
			var lockIcon = posLock != null ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock;
			var lockTooltip = posLock != null ? "Unlock camera position" : "Lock camera position";
			if (GuiHelpers.IconButtonTooltip(lockIcon, lockTooltip, default, "CamPosLock"))
				CameraService.SetPositionLock(addr, isLocked ? null : pos - offset);

			ImGui.SameLine();

			var posCursor = ImGui.GetCursorPosX();
			ImGui.BeginDisabled(!isLocked);
			ImGui.PushItemWidth(TransformTable.InputsWidth);
			if (Transform.DrawFloat3("##CamWorldPos", ref pos, 0.005f, out _))
				CameraService.SetPositionLock(addr, pos - offset);
			ImGui.PopItemWidth();
			ImGui.EndDisabled();

			// hack to align this properly.
			ImGui.PushStyleColor(ImGuiCol.Button, 0);
			ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);
			ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
			GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Plus, "Offset from base position");
			ImGui.PopStyleColor(3);

			ImGui.SameLine();

			ImGui.SetCursorPosX(posCursor);
			ImGui.PushItemWidth(TransformTable.InputsWidth);
			if (Transform.DrawFloat3("##CamOffset", ref offset, 0.005f, out _))
				CameraService.SetOffset(addr, offset != Vector3.Zero ? offset : null);
			ImGui.PopItemWidth();
		}
	}
}