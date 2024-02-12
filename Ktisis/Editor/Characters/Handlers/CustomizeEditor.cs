using System;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Objects.Enums;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Ktisis.Editor.Characters.Types;
using Ktisis.Scene.Entities.Game;
using Ktisis.Structs.Characters;

namespace Ktisis.Editor.Characters.Handlers;

public class CustomizeEditor(ActorEntity actor) : ICustomizeEditor {
	// Customize state wrappers
	
	public unsafe byte GetCustomization(CustomizeIndex index) {
		if (!actor.IsValid) return 0;
		
		if (this.TryGetFromState(index, out var value))
			return value;
		
		return actor.CharacterBaseEx != null ? actor.CharacterBaseEx->Customize[(uint)index] : (byte)0;
	}

	private bool TryGetFromState(CustomizeIndex index, out byte value) {
		value = 0xFF;
		if (!actor.Appearance.Customize.IsSet(index))
			return false;
		value = actor.Appearance.Customize[index];
		return true;
	}

	public void SetCustomization(CustomizeIndex index, byte value) {
		if (this.SetCustomizeValue(index, value))
			this.UpdateCustomizeData(IsRedrawRequired(index));
	}
	
	private unsafe bool IsCurrentValue(CustomizeIndex index, byte value) {
		var result = true;

		var hasState = this.TryGetFromState(index, out var stateVal);
		if (hasState) result &= value == stateVal;
		
		var hasChara = actor.CharacterBaseEx != null;
		if (hasChara) result &= value == actor.CharacterBaseEx->Customize[(uint)index];
		
		return (hasState || hasChara) && result;
	}
	
	// Customize set handlers

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
		if (!redraw && human != null)
			redraw = !human->UpdateDrawData((byte*)&human->Customize, true);
		if (redraw) actor.Redraw();
	}

	private static bool IsRedrawRequired(CustomizeIndex index) {
		return index is CustomizeIndex.Race
			or CustomizeIndex.Tribe
			or CustomizeIndex.Gender
			or CustomizeIndex.FaceType;
	}
	
	// Eye color / heterochromia

	private bool _isHetero;
	private bool _isHeteroGet;
	
	public void SetHeterochromia(bool enabled) {
		this._isHetero = enabled;
		this._isHeteroGet = true;
		if (enabled) return;
		var col2 = this.GetCustomization(CustomizeIndex.EyeColor2);
		this.SetCustomization(CustomizeIndex.EyeColor, col2);
	}

	public bool GetHeterochromia() {
		var col1 = this.GetCustomization(CustomizeIndex.EyeColor);
		var col2 = this.GetCustomization(CustomizeIndex.EyeColor2);
		if (!this._isHeteroGet) {
			this._isHetero = col1 != col2;
			this._isHeteroGet = true;
		} else {
			this._isHetero |= col1 != col2;
		}
		return this._isHetero;
	}

	public void SetEyeColor(byte value) {
		var batch = this.Prepare().SetCustomization(CustomizeIndex.EyeColor, value);
		if (!this.GetHeterochromia())
			batch.SetCustomization(CustomizeIndex.EyeColor2, value);
		batch.Apply();
	}
	
	// Model ID

	public unsafe uint GetModelId() {
		if (!actor.IsValid)
			throw new Exception($"Actor entity '{actor.Name}' is invalid.");
		return actor.Appearance.ModelId ?? GetGameModel(actor.Character);
	}

	public void SetModelId(uint id, bool redraw = true) {
		if (!actor.IsValid)
			throw new Exception($"Actor entity '{actor.Name}' is invalid.");
		redraw &= this.ModelIdDiffers(id);
		actor.Appearance.ModelId = id;
		if (redraw) actor.Redraw();
	}

	private unsafe bool ModelIdDiffers(uint id)
		=> id != (actor.Appearance.ModelId ?? GetGameModel(actor.Character));
	
	private unsafe static uint GetGameModel(Character* chara) {
		if (chara == null)
			throw new Exception("Character is null.");
		return (uint)chara->CharacterData.ModelCharaId;
	}
	
	// Set GameObject state (for spawned actors)

	public unsafe void ApplyStateToGameObject() {
		if (!actor.IsValid || actor.Character == null) return;
		for (var i = 0; i < CustomizeContainer.Size; i++) {
			var value = this.GetCustomization((CustomizeIndex)i);
			actor.Character->DrawData.CustomizeData.Data[i] = value;
		}
	}

	// Batch setter

	public ICustomizeBatch Prepare() => new CustomizeBatch(this);

	private class CustomizeBatch(CustomizeEditor editor) : ICustomizeBatch {
		private readonly Dictionary<CustomizeIndex, byte> Values = new();
		private uint? ModelId;

		public ICustomizeBatch SetCustomization(CustomizeIndex index, byte value) {
			this.Values[index] = value;
			return this;
		}

		public ICustomizeBatch SetIfNotNull(CustomizeIndex index, byte? value) {
			if (value == null) return this;
			this.SetCustomization(index, value.Value);
			return this;
		}

		public ICustomizeBatch SetModelId(uint id) {
			this.ModelId = id;
			return this;
		}
		
		public void Apply() {
			var redraw = false;
			foreach (var (index, value) in this.Values) {
				if (editor.IsCurrentValue(index, value)) continue;
				redraw |= editor.SetCustomizeValue(index, value) && IsRedrawRequired(index);
			}
			if (this.ModelId != null) {
				redraw |= editor.ModelIdDiffers(this.ModelId.Value);
				editor.SetModelId(this.ModelId.Value, false);
			}
			editor.UpdateCustomizeData(redraw);
		}
	}
}
