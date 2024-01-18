using Dalamud.Game.ClientState.Objects.Enums;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Editor.Characters.Types;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Editor.Characters.Handlers;

public class CustomizeEditor : ICustomizeEditor {
	public unsafe byte GetCustomization(ActorEntity actor, CustomizeIndex index) {
		if (!actor.IsValid || actor.CharacterBaseEx == null) return 0;
		if (actor.Appearance.Customize.IsSet(index))
			return actor.Appearance.Customize[index];
		return actor.CharacterBaseEx->Customize[(uint)index];
	}

	public unsafe void SetCustomization(ActorEntity actor, CustomizeIndex index, byte value) {
		if (!actor.IsValid) return;
		actor.Appearance.Customize[index] = value;

		var chara = actor.CharacterBaseEx;
		if (chara == null) return;

		chara->Customize[(uint)index] = value;

		if (chara->Base.GetModelType() != CharacterBase.ModelType.Human) return;
		var human = (Human*)chara;

		var redraw = IsRedrawRequired(index);
		if (!redraw)
			redraw = !human->UpdateDrawData((byte*)&human->Customize, true);

		if (redraw) actor.Redraw();
	}

	private static bool IsRedrawRequired(CustomizeIndex index) {
		return index is CustomizeIndex.Race
			or CustomizeIndex.Tribe
			or CustomizeIndex.Gender
			or CustomizeIndex.FaceType;
	}
}
