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
using Ktisis.Interface.Widgets;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities.Character;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Factory.Builders;
using Ktisis.Scene.Modules.Actors;
using Ktisis.Scene.Types;
using Ktisis.Structs.Actors;
using Ktisis.Data.Config.Sections;

namespace Ktisis.Scene.Entities.Game;

public class ActorEntity : CharaEntity, IDeletable {
	public readonly IGameObject Actor;

	public bool IsManaged { get; set; }

	public unsafe bool IsHidden {
		get {
			var chara = (CharacterEx*)this.Character;
			return chara != null && chara->Opacity == 0.0f;
		}
		set {
			var chara = (CharacterEx*)this.Character;
			if (chara != null)
				if (chara->Opacity != 0.0f)
					chara->Opacity = 0.0f;
				else
					chara->Opacity = 1.0f;
		}
	}

	private bool DefaultsInitialized = false;

	public override bool IsValid => base.IsValid && this.Actor.IsValid();

	public ActorGaze? Gaze;

	private readonly Dictionary<string, PresetState> _presetStates = new();

	private string Anonymized;
	private string RealName;
	public override string Name {
		get { return this.Scene.Context.Config.Editor.IncognitoPlayerNames ? this.Anonymized : this.RealName; }
		set { this.RealName = value; }
	}
 

	public ActorEntity(
		ISceneManager scene,
		IPoseBuilder pose,
		IGameObject actor
	) : base(scene, pose) {
		this.Type = EntityType.Actor;
		this.Actor = actor;
		this.Anonymized = actor.GetNameOrFallback(this.Scene.Context, true);
		PresetConfig.PresetRemovedEvent += RemovePreset;
	}

	private void RemovePreset(string presetName) {
		if (_presetStates.ContainsKey(presetName))
			TogglePreset(presetName, false);
	}

	// Update handler

	public override void Update() {
		if (!this.IsObjectValid) return;
		this.UpdateChara();
		base.Update();

		// after we're drawing, run the default presetter once
		if (!this.DefaultsInitialized)
			this.SetDefaultPresets();
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

	public void ToggleHidden() => this.IsHidden = !this.IsHidden;

	// Deletable

    public bool Delete() {
        this.Scene.GetModule<ActorModule>().Delete(this);
        PresetConfig.PresetRemovedEvent -= RemovePreset;
        return false;
    }
	
    //Presets
    public IEnumerable<(string name, PresetState isEnabled)> GetPresets() {
		var presets = this.Scene.Context.Config.Presets.Presets.Keys;

		foreach (var preset in presets)
		{
			yield return (preset, this._presetStates.GetValueOrDefault(preset, PresetState.Disabled));
		}
	}

	public bool TogglePreset(string presetName, bool? state = null) {
		//check if key exists
		if (!this.Scene.Context.Config.Presets.Presets.TryGetValue(presetName, out var preset))
			return false;

		var op = state ?? this._presetStates.GetValueOrDefault(presetName, PresetState.Disabled) == PresetState.Disabled;

		this.ToggleView(preset, op);

		if (op)
		{
			this._presetStates[presetName] = PresetState.Enabled;
		} else {
			this._presetStates.Remove(presetName);
		}

		EnsurePresetVisibility();
		CheckImplicitlyEnabled();

		return true;
	}

	private void SetDefaultPresets() {
		var allBones = this.Recurse().OfType<BoneNode>().ToList()!;
		if (!allBones.Any())
			return;

		var presets = this.Scene.Context.Config.Presets.DefaultPresets;
		foreach (var preset in presets) {
			Ktisis.Log.Debug($"toggling default preset {preset}");
			this.TogglePreset(preset, true);
		}
		this.DefaultsInitialized = true;
	}

	private void EnsurePresetVisibility() {
		var bones = new HashSet<string>(128);
		foreach (var presetName in _presetStates.Where(s => s.Value == PresetState.Enabled).Select(s => s.Key)) {
			if (!this.Scene.Context.Config.Presets.Presets.TryGetValue(presetName, out var preset)) {
				_presetStates.Remove(presetName);
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
		this._presetStates[presetName] = PresetState.Enabled;
		return true;
	}

	private void CheckImplicitlyEnabled()
	{
		var notEnabled = this.Scene.Context.Config.Presets.Presets.Where(kvp => this._presetStates.GetValueOrDefault(kvp.Key, PresetState.Disabled) != PresetState.Enabled).ToDictionary();

		Ktisis.Log.Debug("Non enabled presets: {0}", string.Join(", ", notEnabled.Keys));
		var allBones = this.Recurse().OfType<BoneNode>().ToList()!;

		foreach (var (preset, boneList) in notEnabled) {
			var currentState = this._presetStates.GetValueOrDefault(preset, PresetState.Disabled);
			var bonesThatExist = allBones.Where(s => boneList.Contains(s.Info.Name)).ToImmutableList();

			Ktisis.Log.Debug("Checking preset {0}", preset);
			Ktisis.Log.Debug("Bones that exist: {0}", string.Join(", ", bonesThatExist.Select(s => s.Name)));
			if (bonesThatExist.IsEmpty)
				continue;

			if (bonesThatExist.All(s => s.Visible)) {
				Ktisis.Log.Debug("Preset {0} is implicitly enabled", preset);
				_presetStates[preset] = PresetState.Implicit;
			}

			if (currentState == PresetState.Implicit && !bonesThatExist.All(s => s.Visible)) {
				Ktisis.Log.Debug("Preset {0} is implicitly disabled", preset);
				this._presetStates.Remove(preset);
			}
		}
	}

	// Gaze
	public unsafe void SetActorGazeTarget(ActorEntity? otherActor) {
		var targetId = otherActor.CsGameObject->GetGameObjectId();
		// for PCs, set hard target and for non-PCs, bail if they have no gaze
		if (this.Actor.IsPcCharacter()) {
			this.Character->SetTargetId(targetId);
			this.Character->SetSoftTargetId(targetId);
			return;
		} else if (this.GetActorGazeTarget() == 0)
			return;

		// for a non-pc with a gaze already, overwrite gaze target where it exists
		var chara = (CharacterEx*)this.Character;
		if (chara == null) return;

		if (this.Gaze == null) this.Gaze = chara->Gaze;
		var gaze = (ActorGaze)this.Gaze;

		for (var i = 0; i < 3; i++) {
			var type = (GazeControl)i;
			var ctrl = gaze[type];
			if (ctrl.TargetId.Type > 0 && ctrl.TargetId.ObjectId > 0) {
				ctrl.TargetId = targetId;
				gaze[type] = ctrl;
			}
		}
		this.Gaze = gaze;
	}

	public unsafe uint GetActorGazeTarget() {
		var chara = this.IsValid ? (CharacterEx*)this.Character : null;
		if (chara == null) return 0;

		for (var i = 0; i < 3; i++) {
			var type = (GazeControl)i;
			var baseGaze = chara->Gaze[type];
			if (baseGaze.Mode == GazeMode.Object
				&& baseGaze.TargetId.Type > 0
				&& baseGaze.TargetId.ObjectId >= 201
				&& baseGaze.TargetId.ObjectId <= 243
			)
				return baseGaze.TargetId.ObjectId;
		}

		return 0;
	}
}
