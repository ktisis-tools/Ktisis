using System;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility.Numerics;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Core.Attributes;
using Ktisis.Structs.Characters;

namespace Ktisis.Interface.Components.Actors;

[Transient]
public class CustomizeEditor {
	public CustomizeEditor(
		
	) {
		
	}
	
	public bool Draw(Customize custom) {
		try {
			
		} finally {
			this.DrawFundamentals(custom);
		}
		
		return false;
	}

	private void DrawFundamentals(Customize custom) {
		var width = (ImGui.GetContentRegionAvail() * 0.35f) with { Y = -1 };
		using var _frame = ImRaii.Child("##CustomizeFrame", width, true);
		
		var genderIcon = custom.Gender == Gender.Feminine ? FontAwesomeIcon.Venus : FontAwesomeIcon.Mars;
		if (Buttons.IconButton(genderIcon)) {}
		
		ImGui.SameLine();

		this.DrawTribe(ref custom.Tribe);

		this.DrawSlider("Height", ref custom.Height);
		this.DrawSlider("Tail Length", ref custom.RaceFeatureSize);
		this.DrawSlider("Bust Size", ref custom.BustSize);
	}

	private bool DrawTribe(ref Tribe tribe) {
		using var combo = ImRaii.Combo("Body##CustomizeTribe", tribe.ToString());
		if (!combo.Success) return false;

		var result = false;
		foreach (var value in Enum.GetValues<Tribe>()) {
			var select = ImGui.Selectable(value.ToString());
			if (select) tribe = value;
			result |= select;
		}
		return result;
	}

	private bool DrawSlider(string label, ref byte value) {
		var intValue = (int)value;
		var moved = ImGui.SliderInt(label, ref intValue, 0, 100);
		if (moved) value = (byte)intValue;
		return moved;
	}
}
