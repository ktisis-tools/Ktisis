using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;

using ImGuiNET;

using Ktisis.Util;
using Ktisis.Camera;
using Ktisis.Structs.Actor;

namespace Ktisis.Interface.Windows.Workspace.Tabs {
	public static class CameraTab {
		public static void Draw() {
			ImGui.Spacing();
			
			DrawTargetLock();
			ImGui.Spacing();
			DrawCameraSelect();

			ImGui.EndTabItem();
		}

		private unsafe static void DrawCameraSelect() {
			var avail = ImGui.GetContentRegionAvail().X;
			var style = ImGui.GetStyle();

			var plusSize = GuiHelpers.CalcIconSize(FontAwesomeIcon.Plus);
			var camSize = GuiHelpers.CalcIconSize(FontAwesomeIcon.Camera);

			var active = Services.Camera->GetActiveCamera();
			var cameras = CameraService.GetCameraList();

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

			ImGui.SameLine();
			var toggleFree = GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Camera, "Toggle work camera");
			if (toggleFree) WorkCamera.Toggle();
		}

		private unsafe static void DrawTargetLock() {
			var active = Services.Camera->GetActiveCamera();

			var tarLock = CameraService.GetTargetLock(active) is GameObject actor ? (Actor*)actor.Address : null;
			var isTarLocked = tarLock != null;
			
			var target = tarLock != null ? tarLock : Ktisis.Target;
			if (target == null) return;
			
			var icon = isTarLocked ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock;
			var tooltip = isTarLocked ? "Unlock camera target" : "Lock camera target";
			if (GuiHelpers.IconButtonTooltip(icon, tooltip)) {
				if (isTarLocked)
					CameraService.UnlockTarget(active);
				else
					CameraService.LockTarget(active, target->GameObject.ObjectIndex);
			}
			
			ImGui.SameLine();
			ImGui.BeginDisabled(!isTarLocked);
			ImGui.Text($"Target: {target->GetNameOrId()}");
			ImGui.EndDisabled();
		}
	}
}