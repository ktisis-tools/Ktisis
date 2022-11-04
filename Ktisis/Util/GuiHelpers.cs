using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;
using ImGuizmoNET;

using Dalamud.Interface;
using Dalamud.Interface.Components;

using FFXIVClientStructs.Havok;

namespace Ktisis.Util
{
	internal class GuiHelpers {
		public static bool IconButtonHoldConfirm(FontAwesomeIcon icon, string tooltip, bool isHoldingKey, Vector2 size = default) {
			if (!isHoldingKey) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().DisabledAlpha);
			bool accepting = IconButton(icon, size);
			if (!isHoldingKey) ImGui.PopStyleVar();

			Tooltip(tooltip);

			return accepting && isHoldingKey;
		}

		public static bool IconButtonTooltip(FontAwesomeIcon icon, string tooltip, Vector2 size = default) {
			bool accepting = IconButton(icon, size);
			Tooltip(tooltip);
			return accepting;
		}
		public static bool IconButton(FontAwesomeIcon icon, Vector2 size = default) {
			ImGui.PushFont(UiBuilder.IconFont);
			bool accepting = ImGui.Button(icon.ToIconString() ?? "", size);
			ImGui.PopFont();
			return accepting;
		}

		public static void Icon(FontAwesomeIcon icon, bool enabled = true, Vector4? color = null) {
			string iconText = icon.ToIconString() ?? "";
			int num = 0;
			if (color.HasValue) {
				ImGui.PushStyleColor(ImGuiCol.Button, color.Value);
				num++;
			}

			ImGui.PushFont(UiBuilder.IconFont);
			if (enabled) ImGui.Text(iconText);
			else ImGui.TextDisabled(iconText);
			ImGui.PopFont();
			if (num > 0) {
				ImGui.PopStyleColor(num);
			}
		}
		public static Vector2 CalcIconSize(FontAwesomeIcon icon) {
			ImGui.PushFont(UiBuilder.IconFont);
			var size = ImGui.CalcTextSize(icon.ToIconString());
			ImGui.PopFont();
			return size;
		}
		public static void IconTooltip(FontAwesomeIcon icon, string tooltip, bool enabled = true, Vector4? color = null) {
			Icon(icon, enabled, color);
			Tooltip(tooltip);
		}


		public static void Tooltip(string text) {
			if (ImGui.IsItemHovered()) {
				ImGui.BeginTooltip();
				ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
				ImGui.TextUnformatted(text);
				ImGui.PopTextWrapPos();
				ImGui.EndTooltip();
			}
		}
		// Copy from Dalamud's ToggleButton but with colorizable circle
		public static bool ToggleButton(string id, ref bool v, Vector4 circleColor) {
			RangeAccessor<Vector4> colors = ImGui.GetStyle().Colors;
			Vector2 cursorScreenPos = ImGui.GetCursorScreenPos();
			ImDrawListPtr windowDrawList = ImGui.GetWindowDrawList();
			float frameHeight = ImGui.GetFrameHeight();
			float num = frameHeight * 1.55f;
			float num2 = frameHeight * 0.5f;
			bool result = false;
			ImGui.InvisibleButton(id, new Vector2(num, frameHeight));
			if (ImGui.IsItemClicked()) {
				v = !v;
				result = true;
			}

			if (ImGui.IsItemHovered()) {
				windowDrawList.AddRectFilled(cursorScreenPos, new Vector2(cursorScreenPos.X + num, cursorScreenPos.Y + frameHeight), ImGui.GetColorU32((!v) ? colors[(int)ImGuiCol.ButtonActive] : new Vector4(0.78f, 0.78f, 0.78f, 1f)), frameHeight * 0.5f);
			} else {
				windowDrawList.AddRectFilled(cursorScreenPos, new Vector2(cursorScreenPos.X + num, cursorScreenPos.Y + frameHeight), ImGui.GetColorU32((!v) ? new Vector4(0.78f, 0.78f, 0.78f, 0.2f) : new Vector4(0.35f, 0.35f, 0.35f, 1f)), frameHeight * 0.5f);
			}

			windowDrawList.AddCircleFilled(new Vector2(cursorScreenPos.X + num2 + (float)(v ? 1 : 0) * (num - num2 * 2f), cursorScreenPos.Y + num2), num2 - 1.5f, ImGui.ColorConvertFloat4ToU32(circleColor));
			return result;
		}

		// this function usually goes with TextRight or inputs/drag to calculate a safe right margin
		public static float GetRightOffset(float calculatedTextSize) {
			return calculatedTextSize
				+ ImGui.GetStyle().ItemSpacing.X
				//+ ImGui.GetStyle().WindowPadding.X
				+ 0.1f; // extra safety
		}
		public static void TextRight(string text, float offset = 0) {
			offset = ImGui.GetContentRegionAvail().X - offset - ImGui.CalcTextSize(text).X;
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
			ImGui.TextUnformatted(text);
		}
		public static void TextCentered(string text) {
			var windowWidth = ImGui.GetWindowSize().X;
			var textWidth = ImGui.CalcTextSize(text).X;

			ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
			ImGui.Text(text);
		}

		public static unsafe void AnimationControls(hkaDefaultAnimationControl* control) {
			var duration = control->hkaAnimationControl.Binding.ptr->Animation.ptr->Duration;
			var durationLimit = duration - 0.05f;

			if (control->hkaAnimationControl.LocalTime >= durationLimit)
				control->hkaAnimationControl.LocalTime = 0f;

			ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - GetRightOffset(ImGui.CalcTextSize("Speed").X));
			ImGui.SliderFloat("Seek", ref control->hkaAnimationControl.LocalTime, 0, durationLimit);
			ImGui.SliderFloat("Speed", ref control->PlaybackSpeed, 0f, 0.999f);
			ImGui.PopItemWidth();
		}


		public static float CalcContrastRatio(uint background, uint foreground) {
			// https://github.com/ocornut/imgui/issues/3798
			float sa0 = ((background >> 24) & 0xFF);
			float sa1 = ((foreground >> 24) & 0xFF);
			float sr = 0.2126f / 255.0f;
			float sg = 0.7152f / 255.0f;
			float sb = 0.0722f / 255.0f;
			float contrastRatio =
				(sr * sa0 * ((background >> 16) & 0xFF) +
					sg * sa0 * ((background >> 8) & 0xFF) +
					sb * sa0 * ((background >> 0) & 0xFF) + 0.05f) /
				(sr * sa1 * ((foreground >> 16) & 0xFF) +
					sg * sa1 * ((foreground >> 8) & 0xFF) +
					sb * sa1 * ((foreground >> 0) & 0xFF) + 0.05f);
			if (contrastRatio < 1.0f)
				return 1.0f / contrastRatio;
			return contrastRatio;
		}
	}
}