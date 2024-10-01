using System;

using Dalamud.Game.ClientState.Objects.Enums;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Ktisis.Data.Files;
using Ktisis.Editor.Characters.State;
using Ktisis.Editor.Characters.Types;
using Ktisis.GameData.Excel.Types;
using Ktisis.Scene.Entities.Game;
using Ktisis.Structs.Actors;
using Ktisis.Structs.Characters;

namespace Ktisis.Editor.Characters;

[Flags]
public enum SaveModes {
	None = 0,
	EquipmentGear = 1,
	EquipmentAccessories = 2,
	EquipmentWeapons = 4,
	AppearanceHair = 8,
	AppearanceFace = 16,
	AppearanceBody = 32,
	AppearanceExtended = 64,
	Equipment = EquipmentGear | EquipmentAccessories,
	Appearance = AppearanceHair | AppearanceFace | AppearanceBody,
	All = Equipment | EquipmentWeapons | Appearance
}

public class EntityCharaConverter {
	private readonly ActorEntity _entity;
	private readonly ICustomizeEditor _custom;
	private readonly IEquipmentEditor _equip;
	
	public EntityCharaConverter(
		ActorEntity entity,
		ICustomizeEditor custom,
		IEquipmentEditor equip
	) {
		this._entity = entity;
		this._custom = custom;
		this._equip = equip;
	}
	
	// CharaFile

	public void Apply(CharaFile file, SaveModes modes = SaveModes.All) {
		this.ApplyEquipment(file, modes);
		this.PrepareCustomize(file, modes).Apply();
		this.ApplyMisc(file);
	}

	public CharaFile Save() {
		var file = new CharaFile { Nickname = this._entity.Name };
		this.WriteCustomize(file);
		this.WriteEquipment(file);
		this.WriteMisc(file);
		return file;
	}
	
	// INpcBase

	public void Apply(INpcBase npc, SaveModes modes = SaveModes.All) {
		this.ApplyEquipment(npc, modes);
		this.PrepareCustomize(npc, modes).Apply();
	}
	
	// Customize loading

	private ICustomizeBatch PrepareCustomize(INpcBase npc, SaveModes modes = SaveModes.All) {
		var batch = this._custom.Prepare();
		
		var modesFace = modes.HasFlag(SaveModes.AppearanceFace);
		var modesBody = modes.HasFlag(SaveModes.AppearanceBody);
		var modesHair = modes.HasFlag(SaveModes.AppearanceHair);
		if (!modesFace && !modesBody && !modesHair) return batch;

		if (modesBody && npc.GetModelId() is var modelId && modelId != ushort.MaxValue)
			batch.SetModelId(modelId);

		var custom = npc.GetCustomize();
		if (custom == null) return batch;

		var isInvalid = true;
		for (uint i = 0; i < CustomizeContainer.Size; i++) {
			isInvalid &= custom.Value[i] == 0;
			if (!isInvalid) break;
		}

		if (isInvalid) return batch;

		foreach (var index in Enum.GetValues<CustomizeIndex>()) {
			var apply = index switch {
				CustomizeIndex.FaceType
					or (>= CustomizeIndex.FaceFeatures and <= CustomizeIndex.LipColor)
					or CustomizeIndex.Facepaint
					or CustomizeIndex.FacepaintColor => modesFace,
				CustomizeIndex.HairStyle
					or CustomizeIndex.HairColor
					or CustomizeIndex.HairColor2
					or CustomizeIndex.HasHighlights => modesHair,
				(>= CustomizeIndex.Race and <= CustomizeIndex.Tribe)
					or (>= CustomizeIndex.RaceFeatureSize and <= CustomizeIndex.BustSize) => modesFace || modesBody,
				_ => modesBody
			};

			if (apply) batch.SetCustomization(index, custom.Value[(uint)index]);
		}
		
		return batch;
	}

	private ICustomizeBatch PrepareCustomize(CharaFile file, SaveModes modes = SaveModes.All) {
		var batch = this._custom.Prepare();
		
		var modesFace = modes.HasFlag(SaveModes.AppearanceFace);
		var modesBody = modes.HasFlag(SaveModes.AppearanceBody);

		if (modes.HasFlag(SaveModes.AppearanceHair)) {
			byte? highlights = file.EnableHighlights is bool val ? (byte)(val ? 0x80 : 0x00) : null;
			batch.SetIfNotNull(CustomizeIndex.HairStyle, file.Hair)
				.SetIfNotNull(CustomizeIndex.HairColor, file.HairTone)
				.SetIfNotNull(CustomizeIndex.HairColor2, file.Highlights)
				.SetIfNotNull(CustomizeIndex.HasHighlights, highlights);
		}
		
		if (modesFace || modesBody) {
			batch.SetIfNotNull(CustomizeIndex.Race, (byte?)file.Race)
				.SetIfNotNull(CustomizeIndex.Tribe, (byte?)file.Tribe)
				.SetIfNotNull(CustomizeIndex.Gender, (byte?)file.Gender)
				.SetIfNotNull(CustomizeIndex.ModelType, (byte?)file.Age);
		}

		if (modesFace) {
			batch.SetIfNotNull(CustomizeIndex.FaceType, file.Head)
				.SetIfNotNull(CustomizeIndex.EyeShape, file.Eyes)
				.SetIfNotNull(CustomizeIndex.EyeColor, file.REyeColor)
				.SetIfNotNull(CustomizeIndex.EyeColor2, file.LEyeColor)
				.SetIfNotNull(CustomizeIndex.Eyebrows, file.Eyebrows)
				.SetIfNotNull(CustomizeIndex.NoseShape, file.Nose)
				.SetIfNotNull(CustomizeIndex.JawShape, file.Jaw)
				.SetIfNotNull(CustomizeIndex.LipStyle, file.Mouth)
				.SetIfNotNull(CustomizeIndex.LipColor, file.LipsToneFurPattern)
				.SetIfNotNull(CustomizeIndex.FaceFeaturesColor, file.LimbalEyes)
				.SetIfNotNull(CustomizeIndex.FaceFeatures, (byte?)file.FacialFeatures)
				.SetIfNotNull(CustomizeIndex.Facepaint, file.FacePaint)
				.SetIfNotNull(CustomizeIndex.FacepaintColor, file.FacePaintColor);
		}

		if (modesBody) {
			batch.SetIfNotNull(CustomizeIndex.Height, file.Height)
				.SetIfNotNull(CustomizeIndex.SkinColor, file.Skintone)
				.SetIfNotNull(CustomizeIndex.RaceFeatureSize, file.EarMuscleTailSize)
				.SetIfNotNull(CustomizeIndex.RaceFeatureType, file.TailEarsType)
				.SetIfNotNull(CustomizeIndex.BustSize, file.Bust)
				.SetModelId(file.ModelType);
		}
		
		return batch;
	}
	
	// Equipment loading

	private void ApplyEquipment(INpcBase npc, SaveModes modes = SaveModes.All) {
		if (modes.HasFlag(SaveModes.EquipmentWeapons)) {
			if (npc.GetMainHand() is {} main)
				this._equip.SetWeaponIndex(WeaponIndex.MainHand, main);
			if (npc.GetOffHand() is {} off)
				this._equip.SetWeaponIndex(WeaponIndex.OffHand, off);
		}
		
		var equipGear = modes.HasFlag(SaveModes.EquipmentGear);
		var equipAcc = modes.HasFlag(SaveModes.EquipmentAccessories);
		if (!equipGear && !equipAcc) return;

		var equip = npc.GetEquipment();
		if (equip == null) return;

		var isInvalid = true;
		for (uint i = 0; i < EquipmentContainer.Length; i++) {
			isInvalid &= equip.Value[i].Value == 0;
			if (!isInvalid) break;
		}

		if (isInvalid) return;
		
		foreach (var index in Enum.GetValues<EquipIndex>()) {
			if (index <= EquipIndex.Feet && !equipGear) continue;
			if (index >= EquipIndex.Earring && !equipAcc) break;
			this._equip.SetEquipIndex(index, equip.Value[(uint)index]);
		}
	}
	
	private void ApplyEquipment(CharaFile file, SaveModes modes = SaveModes.All) {
		if (modes.HasFlag(SaveModes.EquipmentWeapons)) {
			this.SetWeaponIndex(file, WeaponIndex.MainHand)
				.SetWeaponIndex(file, WeaponIndex.OffHand);
		}

		var equipGear = modes.HasFlag(SaveModes.EquipmentGear);
		var equipAcc = modes.HasFlag(SaveModes.EquipmentAccessories);
		if (!equipGear && !equipAcc) return;
		
		foreach (var index in Enum.GetValues<EquipIndex>()) {
			if (index <= EquipIndex.Feet && !equipGear) continue;
			if (index >= EquipIndex.Earring && !equipAcc) break;
			if (GetEquipModelId(file, index) is {} model)
				this._equip.SetEquipIndex(index, model);
		}

		file.Glasses ??= new CharaFile.GlassesSave();
		this._equip.SetGlassesId(0, file.Glasses.GlassesId);
	}

	private EntityCharaConverter SetWeaponIndex(CharaFile file, WeaponIndex index) {
		var save = index switch {
			WeaponIndex.MainHand => file.MainHand,
			WeaponIndex.OffHand => file.OffHand,
			_ => null
		};

		if (save == null) return this;
		
		this._equip.SetWeaponIndex(index, new WeaponModelId {
			Id = save.ModelSet,
			Type = save.ModelBase,
			Variant = save.ModelVariant,
			Stain0 = (byte)save.DyeId,
			Stain1 = (byte)save.DyeId2
		});
		
		return this;
	}

	private static EquipmentModelId? GetEquipModelId(CharaFile file, EquipIndex index) {
		var save = index switch {
			EquipIndex.Head => file.HeadGear,
			EquipIndex.Chest => file.Body,
			EquipIndex.Hands => file.Hands,
			EquipIndex.Legs => file.Legs,
			EquipIndex.Feet => file.Feet,
			EquipIndex.Earring => file.Ears,
			EquipIndex.Necklace => file.Neck,
			EquipIndex.Bracelet => file.Wrists,
			EquipIndex.RingLeft => file.LeftRing,
			EquipIndex.RingRight => file.RightRing,
			_ => null
		};

		if (save == null) return null;
		return new EquipmentModelId {
			Id = save.ModelBase,
			Variant = save.ModelVariant,
			Stain0 = save.DyeId,
			Stain1 = save.DyeId2
		};
	}
	
	// Misc loading

	private unsafe void ApplyMisc(CharaFile file) {
		var chara = this._entity.Character;
		if (chara == null) return;

		if (file.Transparency is float opacity)
			((CharacterEx*)chara)->Opacity = opacity;
	}
	
	// Customize saving

	private void WriteCustomize(CharaFile file) {
		file.ModelType = this._custom.GetModelId();
		
		file.Hair = this._custom.GetCustomization(CustomizeIndex.HairStyle);
		file.HairTone = this._custom.GetCustomization(CustomizeIndex.HairColor);
		file.Highlights = this._custom.GetCustomization(CustomizeIndex.HairColor2);
		file.EnableHighlights = (this._custom.GetCustomization(CustomizeIndex.HasHighlights) & 0x80) != 0;

		file.Race = (CharaFile.AnamRace)this._custom.GetCustomization(CustomizeIndex.Race);
		file.Tribe = (CharaFile.AnamTribe)this._custom.GetCustomization(CustomizeIndex.Tribe);
		file.Gender = (Gender)this._custom.GetCustomization(CustomizeIndex.Gender);
		file.Age = (Age)this._custom.GetCustomization(CustomizeIndex.ModelType);

		file.Head = this._custom.GetCustomization(CustomizeIndex.FaceType);
		file.Eyes = this._custom.GetCustomization(CustomizeIndex.EyeShape);
		file.REyeColor = this._custom.GetCustomization(CustomizeIndex.EyeColor);
		file.LEyeColor = this._custom.GetCustomization(CustomizeIndex.EyeColor2);
		file.Eyebrows = this._custom.GetCustomization(CustomizeIndex.Eyebrows);
		file.Nose = this._custom.GetCustomization(CustomizeIndex.NoseShape);
		file.Jaw = this._custom.GetCustomization(CustomizeIndex.JawShape);
		file.Mouth = this._custom.GetCustomization(CustomizeIndex.LipStyle);
		file.LipsToneFurPattern = this._custom.GetCustomization(CustomizeIndex.LipColor);
		file.LimbalEyes = this._custom.GetCustomization(CustomizeIndex.FaceFeaturesColor);
		file.FacialFeatures = (CharaFile.AnamFacialFeature)this._custom.GetCustomization(CustomizeIndex.FaceFeatures);
		file.FacePaint = this._custom.GetCustomization(CustomizeIndex.Facepaint);
		file.FacePaintColor = this._custom.GetCustomization(CustomizeIndex.FacepaintColor);

		file.Height = this._custom.GetCustomization(CustomizeIndex.Height);
		file.Skintone = this._custom.GetCustomization(CustomizeIndex.SkinColor);
		file.EarMuscleTailSize = this._custom.GetCustomization(CustomizeIndex.RaceFeatureSize);
		file.TailEarsType = this._custom.GetCustomization(CustomizeIndex.RaceFeatureType);
		file.Bust = this._custom.GetCustomization(CustomizeIndex.BustSize);
	}
	
	// Equipment saving

	private void WriteEquipment(CharaFile file) {
		file.MainHand = this.SaveWeapon(WeaponIndex.MainHand);
		file.OffHand = this.SaveWeapon(WeaponIndex.OffHand);
		file.HeadGear = this.SaveItem(EquipIndex.Head);
		file.Body = this.SaveItem(EquipIndex.Chest);
		file.Hands = this.SaveItem(EquipIndex.Hands);
		file.Legs = this.SaveItem(EquipIndex.Legs);
		file.Feet = this.SaveItem(EquipIndex.Feet);
		file.Ears = this.SaveItem(EquipIndex.Earring);
		file.Neck = this.SaveItem(EquipIndex.Necklace);
		file.Wrists = this.SaveItem(EquipIndex.Bracelet);
		file.LeftRing = this.SaveItem(EquipIndex.RingLeft);
		file.RightRing = this.SaveItem(EquipIndex.RingRight);
		file.Glasses = this.SaveGlasses();
	}

	private CharaFile.WeaponSave SaveWeapon(WeaponIndex index) {
		var model = this._equip.GetWeaponIndex(index);
		return new CharaFile.WeaponSave(model);
	}

	private CharaFile.ItemSave SaveItem(EquipIndex index) {
		var model = this._equip.GetEquipIndex(index);
		return new CharaFile.ItemSave(model);
	}

	private CharaFile.GlassesSave SaveGlasses() {
		var id = this._equip.GetGlassesId();
		return new CharaFile.GlassesSave(id);
	}
	
	// Misc saving

	private unsafe void WriteMisc(CharaFile file) {
		var chara = this._entity.Character;
		if (chara == null) return;
		
		file.Transparency = ((CharacterEx*)chara)->Opacity;
		file.HeightMultiplier = chara->GameObject.Height;
	}
}
