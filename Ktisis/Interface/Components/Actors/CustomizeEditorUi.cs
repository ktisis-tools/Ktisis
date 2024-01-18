using Dalamud.Game.ClientState.Objects.Enums;

using ImGuiNET;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Characters.Types;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Components.Actors;

[Transient]
public class CustomizeEditorUi {
	public ICustomizeEditor Editor { set; private get; } = null!;
	
	public CustomizeEditorUi(
		
	) {
		
	}
	
	public void Draw(ActorEntity actor) {
		this.DrawSlider("height", actor, CustomizeIndex.Height);
		this.DrawSlider("boobs", actor, CustomizeIndex.BustSize);
	}

	private void DrawSlider(string label, ActorEntity actor, CustomizeIndex index) {
		var intValue = (int)this.Editor.GetCustomization(actor, index);
		if (ImGui.SliderInt(label, ref intValue, 0, 100))
			this.Editor.SetCustomization(actor, index, (byte)intValue);
	}
}
