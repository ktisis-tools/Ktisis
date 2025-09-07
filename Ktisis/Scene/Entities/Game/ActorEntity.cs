using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Common.Extensions;

using CSGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using CSCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

using Ktisis.Editor.Characters.State;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities.Character;
using Ktisis.Scene.Factory.Builders;
using Ktisis.Scene.Modules.Actors;
using Ktisis.Scene.Types;

using Lumina.Excel.Sheets;

namespace Ktisis.Scene.Entities.Game;

public class ActorEntity : CharaEntity, IDeletable {
	public readonly IGameObject Actor;
	
	public bool IsManaged { get; set; }

	public override bool IsValid => base.IsValid && this.Actor.IsValid();

	private HashSet<string> EnabledPresets { get; } = new ();

	public ActorEntity(
		ISceneManager scene,
		IPoseBuilder pose,
		IGameObject actor
	) : base(scene, pose) {
		this.Type = EntityType.Actor;
		this.Actor = actor;
	}
	
	// Update handler

	public override void Update() {
		if (!this.IsObjectValid) return;
		this.UpdateChara();
		base.Update();
	}

	private unsafe void UpdateChara() {
		var chara = this.CharacterBaseEx;
		
		var address = (nint)chara;
		if (this.Address != address)
			this.Address = address;

		if (chara != null && this.Appearance.Wetness is { } wetness)
			chara->Wetness = wetness;
	}
	
	// Appearance
	
	public AppearanceState Appearance { get; } = new();

	private unsafe CustomizeData* GetCustomize() {
		var human = this.GetHuman();
		if (human != null) return &human->Customize;
		var chara = this.Character;
		if (chara != null) return &chara->DrawData.CustomizeData;
		return null;
	}

	public unsafe byte GetCustomizeValue(CustomizeIndex index) {
		if (this.Appearance.Customize.IsSet(index))
			return this.Appearance.Customize[index];

		var chara = this.GetHuman();
		return chara != null ? chara->Customize[(byte)index] : (byte)0;
	}
	
	// Viera ear handling

	public bool IsViera() => this.GetCustomizeValue(CustomizeIndex.Race) == 8;

	public bool TryGetEarId(out byte id) {
		if (!this.IsViera()) {
			id = 0;
			return false;
		}
		id = this.GetCustomizeValue(CustomizeIndex.RaceFeatureType);
		return true;
	}
	
	public bool TryGetEarIdAsChar(out char id) {
		var result = this.TryGetEarId(out var num);
		id = ((char)(96 + num));
		return result;
	}
	
	// GameObject
	
	public unsafe CSGameObject* CsGameObject => (CSGameObject*)this.Actor.Address;

	public unsafe CSCharacter* Character => this.CsGameObject != null && this.CsGameObject->IsCharacter() ? (CSCharacter*)this.CsGameObject : null;
	
	// CharacterBase

	public unsafe override Object* GetObject()
		=> this.CsGameObject != null ? (Object*)this.CsGameObject->DrawObject : null;

	public unsafe override CharacterBase* GetCharacter() {
		if (!this.IsObjectValid) return null;
		var ptr = this.CsGameObject != null ? this.CsGameObject->DrawObject : null;
		if (ptr == null || ptr->Object.GetObjectType() != ObjectType.CharacterBase)
			return null;
		return (CharacterBase*)ptr;
	}

	public unsafe Human* GetHuman() {
		var chara = this.GetCharacter();
		if (chara != null && chara->GetModelType() == CharacterBase.ModelType.Human)
			return (Human*)chara;
		return null;
	}

	public void Redraw() => this.Actor.Redraw();
	 
	// Deletable

	public bool Delete() {
		this.Scene.GetModule<ActorModule>().Delete(this);
		return false;
	}
	
	//Presets
	public IEnumerable<(string name, bool isEnabled)> GetPresets() {
		var presets = this.Scene.Context.Config.Presets.Presets.Keys;

		foreach (var preset in presets) {
			yield return (preset, this.EnabledPresets.Contains(preset));
		}
	}
	
	public bool TogglePreset(string presetName, bool? state = null) {
		//check if key exists
		if (!this.Scene.Context.Config.Presets.Presets.TryGetValue(presetName, out var preset))
			return false;

		var op = state ?? !this.EnabledPresets.Contains(presetName);
		
		this.ToggleView(preset, op);

		if (op) {
			this.EnabledPresets.Add(presetName);
		} else {
			this.EnabledPresets.Remove(presetName);
		}

		EnsurePresetVisibility();
		
		return true;
	}

	private void EnsurePresetVisibility() {
		var bones = new HashSet<string>(128);
		foreach (var presetName in EnabledPresets) {
			if (!this.Scene.Context.Config.Presets.Presets.TryGetValue(presetName, out var preset)) {
				EnabledPresets.Remove(presetName);
				continue;
			}


			foreach (var bone in preset) {
				bones.Add(bone);
			}
		}
		
		this.ToggleView(bones.ToImmutableHashSet(), true);
	}

	public bool SavePreset(string presetName) {
		if (this.Scene.Context.Config.Presets.Presets.ContainsKey(presetName)) {
			return false;
		}

		var bones = this.GetEnabledBones();
		if (bones.IsEmpty) {
			Ktisis.Log.Warning("No bones selected.");
			return false;
		}
		
		this.Scene.Context.Config.Presets.Presets[presetName] = bones;
		this.EnabledPresets.Add(presetName);
		return true;
	}
}
