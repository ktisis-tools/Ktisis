using System;

using FFXIVClientStructs.FFXIV.Client.System.Framework;

using Dalamud.Bindings.ImGui;

using Ktisis.Structs.Env;

namespace Ktisis.Interface.Widgets.Environment;

public static class DayTimeControls {
	public const float MaxTime = 60 * 60 * 24; // 86400
	
	public unsafe static bool DrawTime(EnvManagerEx* env, out float time) {
		time = 0;
		if (env == null) return false;
		time = env->_base.DayTimeSeconds;

		var dateTime = new DateTime().AddSeconds(time);
		var slider = ImGui.SliderFloat("##TimeControls_Slider", ref time, 0, MaxTime, dateTime.ToShortTimeString(), ImGuiSliderFlags.NoInput);

		ImGui.SameLine();

		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		var drag = ImGui.DragFloat("##TimeControls_Drag", ref time, 10, 0, MaxTime, "%.0f");

		return slider || drag;
	}

	public unsafe static bool DrawDay(EnvManagerEx* env, out int day) {
		day = CalculateDay(env);
		return ImGui.SliderInt("##MoonPhase", ref day, 0, 30);
	}

	public unsafe static int CalculateDay(EnvManagerEx* env) {
		var clientTime = &Framework.Instance()->ClientTime;
		return (int)Math.Ceiling((clientTime->EorzeaTime - env->_base.DayTimeSeconds) / MaxTime) % 32;
	}
}
