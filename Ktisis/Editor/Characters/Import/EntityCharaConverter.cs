using System;

using Dalamud.Game.ClientState.Objects.Enums;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Ktisis.Data.Files;
using Ktisis.Editor.Characters.Handlers;
using Ktisis.Editor.Characters.State;
using Ktisis.Editor.Characters.Types;
using Ktisis.Scene.Entities.Game;
using Ktisis.Structs.Characters;

namespace Ktisis.Editor.Characters.Import;

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
	private readonly string Name;
	private readonly ICustomizeEditor _custom;
	private readonly IEquipmentEditor _equip;
	
	public EntityCharaConverter(
		ActorEntity actor
	) {
		this.Name = actor.Name;
		this._custom = new CustomizeEditor(actor);
		this._equip = new EquipmentEditor(actor);
	}

	public void Apply(CharaFile file, SaveModes modes = SaveModes.All) {
		this.ApplyEquipment(file, modes);
		this.PrepareCustomize(file, modes).Apply();
	}

	public CharaFile Save() {
		var file = new CharaFile { Nickname = this.Name };
		this.WriteCustomize(file);
		this.WriteEquipment(file);
		return file;
	}
	
	// Customize loading

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
				.SetIfNotNull(CustomizeIndex.BustSize, file.Bust);
		}
		
		return batch;
	}
	
	// Equipment loading
	
	public void ApplyEquipment(CharaFile file, SaveModes modes = SaveModes.All) {
		if (modes.HasFlag(SaveModes.EquipmentWeapons)) {
			this.SetWeaponIndex(file, WeaponIndex.MainHand)
				.SetWeaponIndex(file, WeaponIndex.OffHand);
		}

		if (modes.HasFlag(SaveModes.EquipmentGear)) {
			this.SetEquipIndex(file, EquipIndex.Head)
				.SetEquipIndex(file, EquipIndex.Chest)
				.SetEquipIndex(file, EquipIndex.Hands)
				.SetEquipIndex(file, EquipIndex.Legs)
				.SetEquipIndex(file, EquipIndex.Feet);
		}

		if (modes.HasFlag(SaveModes.EquipmentAccessories)) {
			this.SetEquipIndex(file, EquipIndex.Earring)
				.SetEquipIndex(file, EquipIndex.Necklace)
				.SetEquipIndex(file, EquipIndex.Bracelet)
				.SetEquipIndex(file, EquipIndex.RingLeft)
				.SetEquipIndex(file, EquipIndex.RingRight);
		}
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
			Stain = (byte)save.DyeId
		});
		
		return this;
	}

	private EntityCharaConverter SetEquipIndex(CharaFile file, EquipIndex index) {
		var model = GetEquipModelId(file, index);
		if (model != null)
			this._equip.SetEquipIndex(index, model.Value);
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
			Stain = save.DyeId
		};
	}
	
	// Customize saving

	private void WriteCustomize(CharaFile file) {
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
	}

	private CharaFile.WeaponSave SaveWeapon(WeaponIndex index) {
		var model = this._equip.GetWeaponIndex(index);
		return new CharaFile.WeaponSave(model);
	}

	private CharaFile.ItemSave SaveItem(EquipIndex index) {
		var model = this._equip.GetEquipIndex(index);
		return new CharaFile.ItemSave(model);
	}
}
