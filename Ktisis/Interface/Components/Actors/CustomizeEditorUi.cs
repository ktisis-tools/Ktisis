using Dalamud.Game.ClientState.Objects.Enums;

using ImGuiNET;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Characters.Types;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Components.Actors;

[Transient]
public class CustomizeEditorUi {
	public CustomizeEditorUi(
		
	) {
		
	}
	
	public void Draw(ICustomizeEditor editor, ActorEntity actor) {
		this.DrawSlider("boobs", editor, actor, CustomizeIndex.BustSize);
	}

	private void DrawSlider(string label, ICustomizeEditor editor, ActorEntity actor, CustomizeIndex index) {
		var intValue = (int)editor.GetCustomization(actor, index);
		if (ImGui.SliderInt(label, ref intValue, 0, 100))
			editor.SetCustomization(actor, index, (byte)intValue);
	}
}
