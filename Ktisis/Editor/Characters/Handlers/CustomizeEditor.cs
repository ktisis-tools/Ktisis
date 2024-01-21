using System.Collections.Generic;

using Dalamud.Game.ClientState.Objects.Enums;

using Ktisis.Editor.Characters.Types;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Editor.Characters.Handlers;

public class CustomizeEditor(ActorEntity actor) : ICustomizeEditor {
	// Customize wrappers
	
	public unsafe byte GetCustomization(CustomizeIndex index) {
		if (!actor.IsValid || actor.CharacterBaseEx == null) return 0;
		if (actor.Appearance.Customize.IsSet(index))
			return actor.Appearance.Customize[index];
		return actor.CharacterBaseEx->Customize[(uint)index];
	}

	public void SetCustomization(CustomizeIndex index, byte value) {
		if (this.SetCustomizeValue(index, value))
			this.UpdateCustomizeData(IsRedrawRequired(index));
	}

	private unsafe bool SetCustomizeValue(CustomizeIndex index, byte value) {
		if (!actor.IsValid) return false;
		actor.Appearance.Customize[index] = value;

		var chara = actor.CharacterBaseEx;
		if (chara == null) return false;

		chara->Customize[(uint)index] = value;

		return true;
	}

	private unsafe void UpdateCustomizeData(bool redraw) {
		var human = actor.GetHuman();
		if (human == null) return;
		
		if (!redraw)
			redraw = !human->UpdateDrawData((byte*)&human->Customize, true);
		if (redraw)
			actor.Redraw();
	}

	private static bool IsRedrawRequired(CustomizeIndex index) {
		return index is CustomizeIndex.Race
			or CustomizeIndex.Tribe
			or CustomizeIndex.Gender
			or CustomizeIndex.FaceType;
	}
	
	// Batch setter

	public ICustomizeBatch Prepare() => new CustomizeBatch(this);

	private class CustomizeBatch(CustomizeEditor editor) : ICustomizeBatch {
		private readonly Dictionary<CustomizeIndex, byte> Values = new();

		public ICustomizeBatch SetCustomization(CustomizeIndex index, byte value) {
			this.Values[index] = value;
			return this;
		}
		
		public void Dispatch() {
			var redraw = false;
			foreach (var (index, value) in this.Values)
				redraw |= editor.SetCustomizeValue(index, value) && IsRedrawRequired(index);
			editor.UpdateCustomizeData(redraw);
		}
	}
}
