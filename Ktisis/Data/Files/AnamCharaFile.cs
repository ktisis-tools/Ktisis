using System;
using System.Numerics;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Objects.Enums;

using Ktisis.Structs.Actor;

namespace Ktisis.Data.Files {
	public class AnamCharaFile {
		private static Dictionary<string, string> EnumConversions = new() {
			{ "LegacyTattoo", "Legacy" },
			{ "Lalafel", "Lalafell" },
			{ "SeekerOfTheSun", "SunSeeker" },
			{ "KeeperOfTheMoon", "MoonKeeper" },
			{ "Helions", "Helion" },
			{ "TheLost", "Lost" }
		};

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
			Appearance = AppearanceHair | AppearanceFace | AppearanceBody | AppearanceExtended,

			All = EquipmentGear | EquipmentAccessories | EquipmentWeapons | AppearanceHair | AppearanceFace | AppearanceBody | AppearanceExtended,
		}

		public string FileExtension => ".chara";
		public string TypeName => "Anamnesis Character File";

		public SaveModes SaveMode { get; set; } = SaveModes.All;

		public string? Nickname { get; set; } = null;
		public uint ModelType { get; set; } = 0;
		public ObjectKind ObjectKind { get; set; } = ObjectKind.None;

		// appearance
		public Race? Race { get; set; }
		public Gender? Gender { get; set; }
		public Age? Age { get; set; }
		public byte? Height { get; set; }
		public Tribe? Tribe { get; set; }
		public byte? Head { get; set; }
		public byte? Hair { get; set; }
		public bool? EnableHighlights { get; set; }
		public byte? Skintone { get; set; }
		public byte? REyeColor { get; set; }
		public byte? HairTone { get; set; }
		public byte? Highlights { get; set; }
		public FacialFeature? FacialFeatures { get; set; }
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
			this.Nickname = actor.Name;
			this.ModelType = actor.ModelId;
			this.ObjectKind = (ObjectKind)actor.GameObject.ObjectKind;

			this.SaveMode = mode;

			if (this.IncludeSection(SaveModes.EquipmentWeapons, mode)) {
				this.MainHand = new WeaponSave(actor.MainHand.Equip);
				////this.MainHand.Color = actor.GetValue(Offsets.Main.MainHandColor);
				////this.MainHand.Scale = actor.GetValue(Offsets.Main.MainHandScale);

				this.OffHand = new WeaponSave(actor.OffHand.Equip);
				////this.OffHand.Color = actor.GetValue(Offsets.Main.OffhandColor);
				////this.OffHand.Scale = actor.GetValue(Offsets.Main.OffhandScale);
			}

			if (this.IncludeSection(SaveModes.EquipmentGear, mode)) {
				this.HeadGear = new ItemSave(actor.Equipment.Head);
				this.Body = new ItemSave(actor.Equipment.Chest);
				this.Hands = new ItemSave(actor.Equipment.Hands);
				this.Legs = new ItemSave(actor.Equipment.Legs);
				this.Feet = new ItemSave(actor.Equipment.Feet);
			}

			if (this.IncludeSection(SaveModes.EquipmentAccessories, mode)) {
				this.Ears = new ItemSave(actor.Equipment.Earring);
				this.Neck = new ItemSave(actor.Equipment.Necklace);
				this.Wrists = new ItemSave(actor.Equipment.Bracelet);
				this.LeftRing = new ItemSave(actor.Equipment.RingLeft);
				this.RightRing = new ItemSave(actor.Equipment.RingRight);
			}

			if (this.IncludeSection(SaveModes.AppearanceHair, mode)) {
				this.Hair = actor.Customize.HairStyle;
				this.EnableHighlights = (actor.Customize.HasHighlights & 0x80) != 0;
				this.HairTone = actor.Customize.HairColor;
				this.Highlights = actor.Customize.HairColor2;
				/*this.HairColor = actor.ModelObject?.ExtendedAppearance?.HairColor;
				this.HairGloss = actor.ModelObject?.ExtendedAppearance?.HairGloss;
				this.HairHighlight = actor.ModelObject?.ExtendedAppearance?.HairHighlight;*/
			}

			if (this.IncludeSection(SaveModes.AppearanceFace, mode) || this.IncludeSection(SaveModes.AppearanceBody, mode)) {
				this.Race = actor.Customize.Race;
				this.Gender = actor.Customize.Gender;
				this.Tribe = actor.Customize.Tribe;
				this.Age = actor.Customize.Age;
			}

			if (this.IncludeSection(SaveModes.AppearanceFace, mode)) {
				this.Head = actor.Customize.FaceType;
				this.REyeColor = actor.Customize.EyeColor;
				this.LimbalEyes = actor.Customize.FaceFeaturesColor;
				this.FacialFeatures = (FacialFeature)actor.Customize.FaceFeatures;
				this.Eyebrows = actor.Customize.Eyebrows;
				this.LEyeColor = actor.Customize.EyeColor2;
				this.Eyes = actor.Customize.EyeShape;
				this.Nose = actor.Customize.NoseShape;
				this.Jaw = actor.Customize.JawShape;
				this.Mouth = actor.Customize.LipStyle;
				this.LipsToneFurPattern = actor.Customize.RaceFeatureType;
				this.FacePaint = (byte)actor.Customize.Facepaint;
				this.FacePaintColor = actor.Customize.FacepaintColor;
				/*this.LeftEyeColor = actor.ModelObject?.ExtendedAppearance?.LeftEyeColor;
				this.RightEyeColor = actor.ModelObject?.ExtendedAppearance?.RightEyeColor;
				this.LimbalRingColor = actor.ModelObject?.ExtendedAppearance?.LimbalRingColor;
				this.MouthColor = actor.ModelObject?.ExtendedAppearance?.MouthColor;*/
			}

			if (this.IncludeSection(SaveModes.AppearanceBody, mode)) {
				this.Height = actor.Customize.Height;
				this.Skintone = actor.Customize.SkinColor;
				this.EarMuscleTailSize = actor.Customize.RaceFeatureSize;
				this.TailEarsType = actor.Customize.RaceFeatureType;
				this.Bust = actor.Customize.BustSize;

				unsafe { this.HeightMultiplier = actor.Model->Height; }
				/*this.SkinColor = actor.ModelObject?.ExtendedAppearance?.SkinColor;
				this.SkinGloss = actor.ModelObject?.ExtendedAppearance?.SkinGloss;
				this.MuscleTone = actor.ModelObject?.ExtendedAppearance?.MuscleTone;
				this.BustScale = actor.ModelObject?.Bust?.Scale;
				this.Transparency = actor.Transparency;*/
			}
		}

		public void Apply(Actor actor, SaveModes mode, bool allowRefresh = true) {
			if (this.Tribe != null && !Enum.IsDefined((Tribe)this.Tribe))
				throw new Exception($"Invalid tribe: {this.Tribe} in appearance file");

			if (this.Race != null && !Enum.IsDefined((Race)this.Race))
				throw new Exception($"Invalid race: {this.Race} in appearance file");

			//if (actor.CanRefresh) {
				//actor.EnableReading = false;

				actor.ModelId = this.ModelType;
				////actor.ObjectKind = this.ObjectKind;

				if (this.IncludeSection(SaveModes.EquipmentWeapons, mode)) {
					this.MainHand?.Write(actor.MainHand.Equip, true);
					this.OffHand?.Write(actor.OffHand.Equip, false);
				}

				if (this.IncludeSection(SaveModes.EquipmentGear, mode)) {
					this.HeadGear?.Write(actor.Equipment.Head);
					this.Body?.Write(actor.Equipment.Chest);
					this.Hands?.Write(actor.Equipment.Hands);
					this.Legs?.Write(actor.Equipment.Legs);
					this.Feet?.Write(actor.Equipment.Feet);
				}

				if (this.IncludeSection(SaveModes.EquipmentAccessories, mode)) {
					this.Ears?.Write(actor.Equipment.Earring);
					this.Neck?.Write(actor.Equipment.Necklace);
					this.Wrists?.Write(actor.Equipment.Bracelet);
					this.RightRing?.Write(actor.Equipment.RingRight);
					this.LeftRing?.Write(actor.Equipment.RingLeft);
				}

				if (this.IncludeSection(SaveModes.AppearanceHair, mode)) {
					if (this.Hair != null)
						actor.Customize.HairStyle = (byte)this.Hair;

					if (this.EnableHighlights != null)
						actor.Customize.HasHighlights = (byte)((bool)this.EnableHighlights ? 0x80 : 0);

					if (this.HairTone != null)
						actor.Customize.HairColor = (byte)this.HairTone;

					if (this.Highlights != null)
						actor.Customize.HairColor2 = (byte)this.Highlights;
				}

				if (this.IncludeSection(SaveModes.AppearanceFace, mode) || this.IncludeSection(SaveModes.AppearanceBody, mode)) {
					if (this.Race != null)
						actor.Customize.Race = (Race)this.Race;

					if (this.Gender != null)
						actor.Customize.Gender = (Gender)this.Gender;

					if (this.Tribe != null)
						actor.Customize.Tribe = (Tribe)this.Tribe;

					if (this.Age != null)
						actor.Customize.Age = (Age)this.Age;
				}

				if (this.IncludeSection(SaveModes.AppearanceFace, mode)) {
					if (this.Head != null)
						actor.Customize.FaceType = (byte)this.Head;

					if (this.REyeColor != null)
						actor.Customize.EyeColor = (byte)this.REyeColor;

					if (this.FacialFeatures != null)
						actor.Customize.FaceFeatures = (byte)this.FacialFeatures;

					if (this.LimbalEyes != null)
						actor.Customize.FaceFeaturesColor = (byte)this.LimbalEyes;

					if (this.Eyebrows != null)
						actor.Customize.Eyebrows = (byte)this.Eyebrows;

					if (this.LEyeColor != null)
						actor.Customize.EyeColor2 = (byte)this.LEyeColor;

					if (this.Eyes != null)
						actor.Customize.EyeShape = (byte)this.Eyes;

					if (this.Nose != null)
						actor.Customize.NoseShape = (byte)this.Nose;

					if (this.Jaw != null)
						actor.Customize.JawShape = (byte)this.Jaw;

					if (this.Mouth != null)
						actor.Customize.LipStyle = (byte)this.Mouth;

					if (this.LipsToneFurPattern != null)
						actor.Customize.LipColor = (byte)this.LipsToneFurPattern;

					if (this.FacePaint != null)
						actor.Customize.Facepaint = (FacialFeature)this.FacePaint;

					if (this.FacePaintColor != null)
						actor.Customize.FacepaintColor = (byte)this.FacePaintColor;
				}

				if (this.IncludeSection(SaveModes.AppearanceBody, mode)) {
					if (this.Height != null)
						actor.Customize.Height = (byte)this.Height;

					if (this.Skintone != null)
						actor.Customize.SkinColor = (byte)this.Skintone;

					if (this.EarMuscleTailSize != null)
						actor.Customize.RaceFeatureSize = (byte)this.EarMuscleTailSize;

					if (this.TailEarsType != null)
						actor.Customize.RaceFeatureType = (byte)this.TailEarsType;

					if (this.Bust != null)
						actor.Customize.BustSize = (byte)this.Bust;
				}

				//if (allowRefresh) {
					//await actor.RefreshAsync();
				//}

				// Setting customize values will reset the extended appearance, which me must read.
				//actor.EnableReading = true;
				//actor.Tick();
			//}

			/*if (actor.ModelObject?.ExtendedAppearance != null) {
				if (this.IncludeSection(SaveModes.AppearanceHair, mode)) {
					actor.ModelObject.ExtendedAppearance.HairColor = this.HairColor ?? actor.ModelObject.ExtendedAppearance.HairColor;
					actor.ModelObject.ExtendedAppearance.HairGloss = this.HairGloss ?? actor.ModelObject.ExtendedAppearance.HairGloss;
					actor.ModelObject.ExtendedAppearance.HairHighlight = this.HairHighlight ?? actor.ModelObject.ExtendedAppearance.HairHighlight;
				}

				if (this.IncludeSection(SaveModes.AppearanceFace, mode)) {
					actor.ModelObject.ExtendedAppearance.LeftEyeColor = this.LeftEyeColor ?? actor.ModelObject.ExtendedAppearance.LeftEyeColor;
					actor.ModelObject.ExtendedAppearance.RightEyeColor = this.RightEyeColor ?? actor.ModelObject.ExtendedAppearance.RightEyeColor;
					actor.ModelObject.ExtendedAppearance.LimbalRingColor = this.LimbalRingColor ?? actor.ModelObject.ExtendedAppearance.LimbalRingColor;
					actor.ModelObject.ExtendedAppearance.MouthColor = this.MouthColor ?? actor.ModelObject.ExtendedAppearance.MouthColor;
				}

				if (this.IncludeSection(SaveModes.AppearanceBody, mode)) {
					actor.ModelObject.ExtendedAppearance.SkinColor = this.SkinColor ?? actor.ModelObject.ExtendedAppearance.SkinColor;
					actor.ModelObject.ExtendedAppearance.SkinGloss = this.SkinGloss ?? actor.ModelObject.ExtendedAppearance.SkinGloss;
					actor.ModelObject.ExtendedAppearance.MuscleTone = this.MuscleTone ?? actor.ModelObject.ExtendedAppearance.MuscleTone;
					actor.Transparency = this.Transparency ?? actor.Transparency;

					if (this.HeightMultiplier.IsValid())
						actor.ModelObject.Height = this.HeightMultiplier ?? actor.ModelObject.Height;

					if (actor.ModelObject.Bust?.Scale != null && Vector.IsValid(this.BustScale)) {
						actor.ModelObject.Bust.Scale = this.BustScale ?? actor.ModelObject.Bust.Scale;
					}
				}
			}*/

			//actor.AutomaticRefreshEnabled = true;
			//actor.EnableReading = true;
		}

		private bool IncludeSection(SaveModes section, SaveModes mode) {
			return this.SaveMode.HasFlag(section) && mode.HasFlag(section);
		}

		[Serializable]
		public class WeaponSave {
			public WeaponSave() {
			}

			public WeaponSave(WeaponEquip from) {
				this.ModelSet = from.Set;
				this.ModelBase = from.Base;
				this.ModelVariant = from.Variant;
				this.DyeId = from.Dye;
			}

			public Vector3 Color { get; set; }
			public Vector3 Scale { get; set; }
			public ushort ModelSet { get; set; }
			public ushort ModelBase { get; set; }
			public ushort ModelVariant { get; set; }
			public byte DyeId { get; set; }

			public void Write(WeaponEquip vm, bool isMainHand) {
				vm.Set = this.ModelSet;

				// sanity check values
				if (vm.Set != 0) {
					vm.Base = this.ModelBase;
					vm.Variant = this.ModelVariant;
					vm.Dye = this.DyeId;
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
				this.ModelBase = from.Id;
				this.ModelVariant = from.Variant;
				this.DyeId = from.Dye;
			}

			public ushort ModelBase { get; set; }
			public byte ModelVariant { get; set; }
			public byte DyeId { get; set; }

			public void Write(ItemEquip vm) {
				vm.Id = this.ModelBase;
				vm.Variant = this.ModelVariant;
				vm.Dye = this.DyeId;
			}
		}
	}
}