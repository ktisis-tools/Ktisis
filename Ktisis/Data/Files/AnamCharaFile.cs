using System;
using System.Numerics;

using Dalamud.Game.ClientState.Objects.Enums;

using Ktisis.Structs.Actor;

namespace Ktisis.Data.Files {
	public class AnamCharaFile {
		// https://github.com/imchillin/Anamnesis/blob/master/Anamnesis/Files/CharacterFile.cs

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

			All = EquipmentGear | EquipmentAccessories | EquipmentWeapons | AppearanceHair | AppearanceFace | AppearanceBody
		}

		public string FileExtension => ".chara";
		public string TypeName => "Ktisis/Anamnesis Character File";

		public SaveModes SaveMode { get; set; } = SaveModes.All;

		public string? Nickname { get; set; } = null;
		public uint ModelType { get; set; } = 0;
		public ObjectKind ObjectKind { get; set; } = ObjectKind.None;

		// appearance
		public AnamRace? Race { get; set; }
		public Gender? Gender { get; set; }
		public Age? Age { get; set; }
		public byte? Height { get; set; }
		public AnamTribe? Tribe { get; set; }
		public byte? Head { get; set; }
		public byte? Hair { get; set; }
		public bool? EnableHighlights { get; set; }
		public byte? Skintone { get; set; }
		public byte? REyeColor { get; set; }
		public byte? HairTone { get; set; }
		public byte? Highlights { get; set; }
		public AnamFacialFeature? FacialFeatures { get; set; }
		public byte? LimbalEyes { get; set; } // facial feature color
		public byte? Eyebrows { get; set; }
		public byte? LEyeColor { get; set; }
		public byte? Eyes { get; set; }
		public byte? Nose { get; set; }
		public byte? Jaw { get; set; }
		public byte? Mouth { get; set; }
		public byte? LipsToneFurPattern { get; set; }
		public byte? EarMuscleTailSize { get; set; }
		public byte? TailEarsType { get; set; }
		public byte? Bust { get; set; }
		public byte? FacePaint { get; set; }
		public byte? FacePaintColor { get; set; }

		// weapons
		public WeaponSave? MainHand { get; set; }
		public WeaponSave? OffHand { get; set; }

		// equipment
		public ItemSave? HeadGear { get; set; }
		public ItemSave? Body { get; set; }
		public ItemSave? Hands { get; set; }
		public ItemSave? Legs { get; set; }
		public ItemSave? Feet { get; set; }
		public ItemSave? Ears { get; set; }
		public ItemSave? Neck { get; set; }
		public ItemSave? Wrists { get; set; }
		public ItemSave? LeftRing { get; set; }
		public ItemSave? RightRing { get; set; }

		// extended appearance
		// NOTE: extended weapon values are stored in the WeaponSave
		public Vector3? SkinColor { get; set; }
		public Vector3? SkinGloss { get; set; }
		public Vector3? LeftEyeColor { get; set; }
		public Vector3? RightEyeColor { get; set; }
		public Vector3? LimbalRingColor { get; set; }
		public Vector3? HairColor { get; set; }
		public Vector3? HairGloss { get; set; }
		public Vector3? HairHighlight { get; set; }
		public Vector4? MouthColor { get; set; }
		public Vector3? BustScale { get; set; }
		public float? Transparency { get; set; }
		public float? MuscleTone { get; set; }
		public float? HeightMultiplier { get; set; }

		public void WriteToFile(Actor actor, SaveModes mode) {
			Nickname = actor.Name;
			ModelType = actor.ModelId;
			ObjectKind = (ObjectKind)actor.GameObject.ObjectKind;

			SaveMode = mode;

			if (IncludeSection(SaveModes.EquipmentWeapons, mode)) {
				MainHand = new WeaponSave(actor.MainHand.Equip);
				////MainHand.Color = actor.GetValue(Offsets.Main.MainHandColor);
				////MainHand.Scale = actor.GetValue(Offsets.Main.MainHandScale);

				OffHand = new WeaponSave(actor.OffHand.Equip);
				////OffHand.Color = actor.GetValue(Offsets.Main.OffhandColor);
				////OffHand.Scale = actor.GetValue(Offsets.Main.OffhandScale);
			}

			if (IncludeSection(SaveModes.EquipmentGear, mode)) {
				HeadGear = new ItemSave(actor.Equipment.Head);
				Body = new ItemSave(actor.Equipment.Chest);
				Hands = new ItemSave(actor.Equipment.Hands);
				Legs = new ItemSave(actor.Equipment.Legs);
				Feet = new ItemSave(actor.Equipment.Feet);
			}

			if (IncludeSection(SaveModes.EquipmentAccessories, mode)) {
				Ears = new ItemSave(actor.Equipment.Earring);
				Neck = new ItemSave(actor.Equipment.Necklace);
				Wrists = new ItemSave(actor.Equipment.Bracelet);
				LeftRing = new ItemSave(actor.Equipment.RingLeft);
				RightRing = new ItemSave(actor.Equipment.RingRight);
			}

			if (IncludeSection(SaveModes.AppearanceHair, mode)) {
				Hair = actor.Customize.HairStyle;
				EnableHighlights = (actor.Customize.HasHighlights & 0x80) != 0;
				HairTone = actor.Customize.HairColor;
				Highlights = actor.Customize.HairColor2;
				/*HairColor = actor.ModelObject?.ExtendedAppearance?.HairColor;
				HairGloss = actor.ModelObject?.ExtendedAppearance?.HairGloss;
				HairHighlight = actor.ModelObject?.ExtendedAppearance?.HairHighlight;*/
			}

			if (IncludeSection(SaveModes.AppearanceFace, mode) || IncludeSection(SaveModes.AppearanceBody, mode)) {
				Race = (AnamRace)actor.Customize.Race;
				Gender = actor.Customize.Gender;
				Tribe = (AnamTribe)actor.Customize.Tribe;
				Age = actor.Customize.Age;
			}

			if (IncludeSection(SaveModes.AppearanceFace, mode)) {
				Head = actor.Customize.FaceType;
				REyeColor = actor.Customize.EyeColor;
				LimbalEyes = actor.Customize.FaceFeaturesColor;
				FacialFeatures = (AnamFacialFeature)actor.Customize.FaceFeatures;
				Eyebrows = actor.Customize.Eyebrows;
				LEyeColor = actor.Customize.EyeColor2;
				Eyes = actor.Customize.EyeShape;
				Nose = actor.Customize.NoseShape;
				Jaw = actor.Customize.JawShape;
				Mouth = actor.Customize.LipStyle;
				LipsToneFurPattern = actor.Customize.LipColor;
				FacePaint = (byte)actor.Customize.Facepaint;
				FacePaintColor = actor.Customize.FacepaintColor;
				/*LeftEyeColor = actor.ModelObject?.ExtendedAppearance?.LeftEyeColor;
				RightEyeColor = actor.ModelObject?.ExtendedAppearance?.RightEyeColor;
				LimbalRingColor = actor.ModelObject?.ExtendedAppearance?.LimbalRingColor;
				MouthColor = actor.ModelObject?.ExtendedAppearance?.MouthColor;*/
			}

			if (IncludeSection(SaveModes.AppearanceBody, mode)) {
				Height = actor.Customize.Height;
				Skintone = actor.Customize.SkinColor;
				EarMuscleTailSize = actor.Customize.RaceFeatureSize;
				TailEarsType = actor.Customize.RaceFeatureType;
				Bust = actor.Customize.BustSize;

				unsafe { HeightMultiplier = actor.Model->Height; }

				/*SkinColor = actor.ModelObject?.ExtendedAppearance?.SkinColor;
				SkinGloss = actor.ModelObject?.ExtendedAppearance?.SkinGloss;
				MuscleTone = actor.ModelObject?.ExtendedAppearance?.MuscleTone;
				BustScale = actor.ModelObject?.Bust?.Scale;
				Transparency = actor.Transparency;*/
			}
		}

		public unsafe void Apply(Actor* actor, SaveModes mode) {
			if (Tribe != null && !Enum.IsDefined((Tribe)Tribe))
				throw new Exception($"Invalid tribe: {Tribe} in appearance file");

			if (Race != null && !Enum.IsDefined((Race)Race))
				throw new Exception($"Invalid race: {Race} in appearance file");

			actor->ModelId = ModelType;

			if (IncludeSection(SaveModes.EquipmentWeapons, mode)) {
				MainHand?.Write(actor->MainHand.Equip, true);
				OffHand?.Write(actor->OffHand.Equip, false);
			}

			if (IncludeSection(SaveModes.EquipmentGear, mode)) {
				HeadGear?.Write(actor, EquipIndex.Head);
				Body?.Write(actor, EquipIndex.Chest);
				Hands?.Write(actor, EquipIndex.Hands);
				Legs?.Write(actor, EquipIndex.Legs);
				Feet?.Write(actor, EquipIndex.Feet);
			}

			if (IncludeSection(SaveModes.EquipmentAccessories, mode)) {
				Ears?.Write(actor, EquipIndex.Earring);
				Neck?.Write(actor, EquipIndex.Necklace);
				Wrists?.Write(actor, EquipIndex.Bracelet);
				RightRing?.Write(actor, EquipIndex.RingRight);
				LeftRing?.Write(actor, EquipIndex.RingLeft);
			}

			var custom = actor->Customize;

			if (IncludeSection(SaveModes.AppearanceHair, mode)) {
				if (Hair != null)
					custom.HairStyle = (byte)Hair;

				if (EnableHighlights != null)
					custom.HasHighlights = (byte)((bool)EnableHighlights ? 0x80 : 0);

				if (HairTone != null)
					custom.HairColor = (byte)HairTone;

				if (Highlights != null)
					custom.HairColor2 = (byte)Highlights;
			}

			if (IncludeSection(SaveModes.AppearanceFace, mode) || IncludeSection(SaveModes.AppearanceBody, mode)) {
				if (Race != null)
					custom.Race = (Race)Race;

				if (Gender != null)
					custom.Gender = (Gender)Gender;

				if (Tribe != null)
					custom.Tribe = (Tribe)Tribe;

				if (Age != null)
					custom.Age = (Age)Age;
			}

			if (IncludeSection(SaveModes.AppearanceFace, mode)) {
				if (Head != null)
					custom.FaceType = (byte)Head;

				if (REyeColor != null)
					custom.EyeColor = (byte)REyeColor;

				if (FacialFeatures != null)
					custom.FaceFeatures = (byte)FacialFeatures;

				if (LimbalEyes != null)
					custom.FaceFeaturesColor = (byte)LimbalEyes;

				if (Eyebrows != null)
					custom.Eyebrows = (byte)Eyebrows;

				if (LEyeColor != null)
					custom.EyeColor2 = (byte)LEyeColor;

				if (Eyes != null)
					custom.EyeShape = (byte)Eyes;

				if (Nose != null)
					custom.NoseShape = (byte)Nose;

				if (Jaw != null)
					custom.JawShape = (byte)Jaw;

				if (Mouth != null)
					custom.LipStyle = (byte)Mouth;

				if (LipsToneFurPattern != null)
					custom.LipColor = (byte)LipsToneFurPattern;

				if (FacePaint != null)
					custom.Facepaint = (FacialFeature)FacePaint;

				if (FacePaintColor != null)
					custom.FacepaintColor = (byte)FacePaintColor;
			}

			if (IncludeSection(SaveModes.AppearanceBody, mode)) {
				if (Height != null)
					custom.Height = (byte)Height;

				if (Skintone != null)
					custom.SkinColor = (byte)Skintone;

				if (EarMuscleTailSize != null)
					custom.RaceFeatureSize = (byte)EarMuscleTailSize;

				if (TailEarsType != null)
					custom.RaceFeatureType = (byte)TailEarsType;

				if (Bust != null)
					custom.BustSize = (byte)Bust;
			}

			actor->ApplyCustomize(custom);

			/*if (actor.ModelObject?.ExtendedAppearance != null) {
				if (IncludeSection(SaveModes.AppearanceHair, mode)) {
					actor.ModelObject.ExtendedAppearance.HairColor = HairColor ?? actor.ModelObject.ExtendedAppearance.HairColor;
					actor.ModelObject.ExtendedAppearance.HairGloss = HairGloss ?? actor.ModelObject.ExtendedAppearance.HairGloss;
					actor.ModelObject.ExtendedAppearance.HairHighlight = HairHighlight ?? actor.ModelObject.ExtendedAppearance.HairHighlight;
				}

				if (IncludeSection(SaveModes.AppearanceFace, mode)) {
					actor.ModelObject.ExtendedAppearance.LeftEyeColor = LeftEyeColor ?? actor.ModelObject.ExtendedAppearance.LeftEyeColor;
					actor.ModelObject.ExtendedAppearance.RightEyeColor = RightEyeColor ?? actor.ModelObject.ExtendedAppearance.RightEyeColor;
					actor.ModelObject.ExtendedAppearance.LimbalRingColor = LimbalRingColor ?? actor.ModelObject.ExtendedAppearance.LimbalRingColor;
					actor.ModelObject.ExtendedAppearance.MouthColor = MouthColor ?? actor.ModelObject.ExtendedAppearance.MouthColor;
				}

				if (IncludeSection(SaveModes.AppearanceBody, mode)) {
					actor.ModelObject.ExtendedAppearance.SkinColor = SkinColor ?? actor.ModelObject.ExtendedAppearance.SkinColor;
					actor.ModelObject.ExtendedAppearance.SkinGloss = SkinGloss ?? actor.ModelObject.ExtendedAppearance.SkinGloss;
					actor.ModelObject.ExtendedAppearance.MuscleTone = MuscleTone ?? actor.ModelObject.ExtendedAppearance.MuscleTone;
					actor.Transparency = Transparency ?? actor.Transparency;

					if (HeightMultiplier.IsValid())
						actor.ModelObject.Height = HeightMultiplier ?? actor.ModelObject.Height;

					if (actor.ModelObject.Bust?.Scale != null && Vector.IsValid(BustScale)) {
						actor.ModelObject.Bust.Scale = BustScale ?? actor.ModelObject.Bust.Scale;
					}
				}
			}*/
		}

		private bool IncludeSection(SaveModes section, SaveModes mode) {
			return SaveMode.HasFlag(section) && mode.HasFlag(section);
		}

		[Serializable]
		public class WeaponSave {
			public WeaponSave() {
			}

			public WeaponSave(WeaponEquip from) {
				ModelSet = from.Set;
				ModelBase = from.Base;
				ModelVariant = from.Variant;
				DyeId = from.Dye;
			}

			public Vector3 Color { get; set; }
			public Vector3 Scale { get; set; }
			public ushort ModelSet { get; set; }
			public ushort ModelBase { get; set; }
			public ushort ModelVariant { get; set; }
			public byte DyeId { get; set; }

			public void Write(WeaponEquip vm, bool isMainHand) {
				// TODO

				vm.Set = ModelSet;

				// sanity check values
				if (vm.Set != 0) {
					vm.Base = ModelBase;
					vm.Variant = ModelVariant;
					vm.Dye = DyeId;
				} else {
					/*if (isMainHand) {
						vm.Set = ItemUtility.EmperorsNewFists.ModelSet;
						vm.Base = ItemUtility.EmperorsNewFists.ModelBase;
						vm.Variant = ItemUtility.EmperorsNewFists.ModelVariant;
					} else {*/
						vm.Set = 0;
						vm.Base = 0;
						vm.Variant = 0;
					//}

					vm.Dye = 0;
					//vm.Dye = ItemUtility.NoneDye.Id;
				}
			}
		}

		[Serializable]
		public class ItemSave {
			public ItemSave() {
			}

			public ItemSave(ItemEquip from) {
				ModelBase = from.Id;
				ModelVariant = from.Variant;
				DyeId = from.Dye;
			}

			public ushort ModelBase { get; set; }
			public byte ModelVariant { get; set; }
			public byte DyeId { get; set; }

			public unsafe void Write(Actor* actor, EquipIndex index) {
				var item = new ItemEquip() {
					Id = ModelBase,
					Variant = ModelVariant,
					Dye = DyeId
				};
				actor->Equip(index, item);
			}
		}

		public enum AnamRace : byte {
			Hyur = 1,
			Elezen = 2,
			Lalafel = 3,
			Miqote = 4,
			Roegadyn = 5,
			AuRa = 6,
			Hrothgar = 7,
			Viera = 8
		}

		public enum AnamTribe : byte {
			Midlander = 1,
			Highlander = 2,
			Wildwood = 3,
			Duskwight = 4,
			Plainsfolk = 5,
			Dunesfolk = 6,
			SeekerOfTheSun = 7,
			KeeperOfTheMoon = 8,
			SeaWolf = 9,
			Hellsguard = 10,
			Raen = 11,
			Xaela = 12,
			Helions = 13,
			TheLost = 14,
			Rava = 15,
			Veena = 16
		}

		[Flags]
		public enum AnamFacialFeature : byte {
			None = 0,
			First = 1,
			Second = 2,
			Third = 4,
			Fourth = 8,
			Fifth = 16,
			Sixth = 32,
			Seventh = 64,
			LegacyTattoo = 128
		}
	}
}
