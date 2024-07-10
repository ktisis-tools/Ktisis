using System;
using System.Numerics;

using Dalamud.Game.ClientState.Objects.Enums;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Ktisis.Data.Json.Converters;
using Ktisis.Structs.Characters;

namespace Ktisis.Data.Files;

// https://github.com/imchillin/Anamnesis/blob/master/Anamnesis/Files/CharacterFile.cs
// https://github.com/ktisis-tools/Ktisis/blob/main/Ktisis/Data/Files/AnamCharaFile.cs

// This format is horrid to work with. Might be worth deprecating in future.

public class CharaFile : JsonFile {
	public new string FileExtension { get; set; } = ".chara";
	public new string TypeName { get; set; } = "Ktisis Character File";

	public const int CurrentVersion = 1;
	
	// Data
	
	[DeserializerDefault(1)] // Assume 1 if not present.
	public new int FileVersion { get; set; } = CurrentVersion;
	
	public string? Nickname { get; set; } = null;
	public uint ModelType { get; set; } = 0;
	public ObjectKind ObjectKind { get; set; } = ObjectKind.None;
	
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
	
	public WeaponSave? MainHand { get; set; }
	public WeaponSave? OffHand { get; set; }
	
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
	
	public Vector3? BustScale { get; set; }
	public float? Transparency { get; set; }
	public float? HeightMultiplier { get; set; }
	
	// Weapon data
	
	[Serializable]
	public class WeaponSave {
		public WeaponSave() { }

		public WeaponSave(WeaponModelId from) {
			this.ModelSet = from.Id;
			this.ModelBase = from.Type;
			this.ModelVariant = from.Variant;
			this.DyeId = from.Stain0;
			this.DyeId2 = from.Stain1;
		}

		public Vector3 Color { get; set; }
		public Vector3 Scale { get; set; }
		public ushort ModelSet { get; set; }
		public ushort ModelBase { get; set; }
		public ushort ModelVariant { get; set; }
		public ushort DyeId { get; set; }
		public ushort DyeId2 { get; set; }
	}
	
	// Item data
	
	[Serializable]
	public class ItemSave {
		public ItemSave() { }

		public ItemSave(EquipmentModelId from) {
			this.ModelBase = from.Id;
			this.ModelVariant = from.Variant;
			this.DyeId = from.Stain0;
			this.DyeId2 = from.Stain1;
		}

		public ushort ModelBase { get; set; }
		public byte ModelVariant { get; set; }
		public byte DyeId { get; set; }
		public byte DyeId2 { get; set; }
	}
	
	// Enums, required due to the way this was serialized.
	
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
