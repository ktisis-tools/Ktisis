using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Keys;
using Dalamud.Bindings.ImGui;

using Ktisis.Interop.Hooks;
using Ktisis.Structs.Input;

namespace Ktisis.Interface.Components {
	public static class KeybindEdit {
		private enum ButtonState {
			None,
			Activated,
			Deactivated
		}
		
		private static string? Activated;
		private static ButtonState State;
		private static List<VirtualKey>? Buffer;

		public static void Draw(string label, Keybind bind) {
			State = ButtonState.None;
			DrawButton(label, bind);
			switch (State) {
				case ButtonState.Activated:
					HandleInput();
					break;
				case ButtonState.Deactivated:
					if (Buffer?.Count > 0)
						bind.Keys = Buffer.ToArray();
					Buffer = null;
					break;
			}
		}

		private static void HandleInput() {
			if (Buffer == null) return;

			ControlHooks.KeyboardCaptureAll = true;

			var keys = Services.KeyState.GetValidVirtualKeys();
			foreach (var key in keys) {
				if ((int)key >= KeyboardState.Length) continue;
				if (ControlHooks.KeyboardState.IsKeyDown(key, true))
					Buffer.Add(key);
			}
		}
		
		private static bool DrawButton(string label, Keybind bind) {
			var pos = ImGui.GetWindowPos() + ImGui.GetCursorPos();
			var size = new Vector2(
				ImGui.CalcItemWidth(),
				ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2
			);

			var activated = Activated == label;
			if (activated) State = ButtonState.Activated;
			var color = ImGui.GetColorU32(activated switch {
				true => ImGuiCol.FrameBgActive,
				false when ImGui.IsMouseHoveringRect(pos, pos + size)
					=> ImGuiCol.FrameBgHovered,
				false => ImGuiCol.FrameBg
			});
			
			ImGui.PushStyleColor(ImGuiCol.Button, color);
			ImGui.PushStyleColor(ImGuiCol.ButtonActive, color);
			ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);

			var isBufferUsed = Buffer?.Count != 0;
			var btnLabel = activated && isBufferUsed ? string.Join("+", Buffer!.Select(k => k.GetFancyName())) : bind.Display();
			ImGui.BeginDisabled(activated && !isBufferUsed);
			var result = ImGui.Button($"{btnLabel}##{label}", size);
			if (result) {
				Activated = label;
				Buffer = new List<VirtualKey>();
			} else if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && activated) {
				State = ButtonState.Deactivated;
				Activated = null;
			}
			ImGui.EndDisabled();

			ImGui.PopStyleColor(3);

			var x = ImGui.GetCursorPosX();
			ImGui.SameLine();
			ImGui.SetCursorPosX(x);
			ImGui.LabelText(label, "");
			
			return result;
		}
	}
}