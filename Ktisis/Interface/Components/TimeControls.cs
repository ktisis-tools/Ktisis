using System;

using ImGuiNET;

using FFXIVClientStructs.FFXIV.Client.System.Framework;

using Ktisis.Interop.Hooks;
using Ktisis.Structs.FFXIV;
using Ktisis.Util;

namespace Ktisis.Interface.Components {
	public static class TimeControls {
		public unsafe static void Draw() {
			if (ImGui.CollapsingHeader("Time Control")) {
				Framework* framework = Framework.Instance();

				ImGui.Checkbox("Lock Time", ref WorldHooks.TimeUpdateDisabled);

				bool isLocked = WorldHooks.TimeUpdateDisabled;
				bool isOverridden = framework->IsEorzeaTimeOverridden;
				long currentTime = isOverridden ? framework->EorzeaTimeOverride : framework->EorzeaTime;

				long timeVal = currentTime % 2764800;
				long secondInDay = timeVal % 86400;
				int timeOfDay = (int)(secondInDay / 60f);
				int dayOfMonth = (int)(Math.Floor(timeVal / 86400f) + 1);
				var displayTime = TimeSpan.FromMinutes(timeOfDay);

				int originalTime = timeOfDay;
				int originalDay = dayOfMonth;

				if (!isLocked) ImGui.BeginDisabled();

				ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - GuiHelpers.WidthMargin() - GuiHelpers.GetRightOffset(ImGui.CalcTextSize("Day of Month").X));
				ImGui.SliderInt("Time of Day", ref timeOfDay, 0, 1439, $"{displayTime.Hours:D2}:{displayTime.Minutes:D2}");
				ImGui.SliderInt("Day of Month", ref dayOfMonth, 1, 31);
				ImGui.PopItemWidth();

				if (!isLocked) ImGui.EndDisabled();


				if (originalTime != timeOfDay || originalDay != dayOfMonth) {
					long newTime = ((timeOfDay * 60) + (86400 * ((byte)(dayOfMonth) - 1)));

					if (isOverridden) framework->EorzeaTimeOverride = newTime;
					framework->EorzeaTime = newTime;
				}
			}
		}
	}
}
