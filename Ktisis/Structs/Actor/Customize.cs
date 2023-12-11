using System;
using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Objects.Enums;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit, Size = 0x1A)]
	public unsafe struct Customize {
		public const int Length = 0x1A;
        
		[FieldOffset(0)] public fixed byte Bytes[Customize.Length];

		// this is auto-generated
		[FieldOffset((int)CustomizeIndex.BustSize)] public byte BustSize;
		[FieldOffset((int)CustomizeIndex.Eyebrows)] public byte Eyebrows;
		[FieldOffset((int)CustomizeIndex.EyeColor)] public byte EyeColor;
		[FieldOffset((int)CustomizeIndex.EyeColor2)] public byte EyeColor2;
		[FieldOffset((int)CustomizeIndex.EyeShape)] public byte EyeShape;
		[FieldOffset((int)CustomizeIndex.FaceFeatures)] public byte FaceFeatures;
		[FieldOffset((int)CustomizeIndex.FaceFeaturesColor)] public byte FaceFeaturesColor;
		[FieldOffset((int)CustomizeIndex.Facepaint)] public FacialFeature Facepaint;
		[FieldOffset((int)CustomizeIndex.FacepaintColor)] public byte FacepaintColor;
		[FieldOffset((int)CustomizeIndex.FaceType)] public byte FaceType;
		[FieldOffset((int)CustomizeIndex.Gender)] public Gender Gender;
		[FieldOffset((int)CustomizeIndex.HairColor)] public byte HairColor;
		[FieldOffset((int)CustomizeIndex.HairColor2)] public byte HairColor2;
		[FieldOffset((int)CustomizeIndex.HairStyle)] public byte HairStyle;
		[FieldOffset((int)CustomizeIndex.HasHighlights)] public byte HasHighlights;
		[FieldOffset((int)CustomizeIndex.Height)] public byte Height;
		[FieldOffset((int)CustomizeIndex.JawShape)] public byte JawShape;
		[FieldOffset((int)CustomizeIndex.LipColor)] public byte LipColor;
		[FieldOffset((int)CustomizeIndex.LipStyle)] public byte LipStyle;
		[FieldOffset((int)CustomizeIndex.ModelType)] public byte ModelType;
		[FieldOffset((int)CustomizeIndex.NoseShape)] public byte NoseShape;
		[FieldOffset((int)CustomizeIndex.Race)] public Race Race;
		[FieldOffset((int)CustomizeIndex.RaceFeatureSize)] public byte RaceFeatureSize;
		[FieldOffset((int)CustomizeIndex.RaceFeatureType)] public byte RaceFeatureType;
		[FieldOffset((int)CustomizeIndex.SkinColor)] public byte SkinColor;
		[FieldOffset((int)CustomizeIndex.Tribe)] public Tribe Tribe;

		[FieldOffset(2)] public Age Age;

		public byte GetRaceTribeIndex()
			=> (byte)((byte)Race * 2 - 1);
		public uint GetMakeIndex() 
			=> (((uint)Race - 1) * 4) + (((uint)Tribe - GetRaceTribeIndex()) * 2) + (uint)Gender; // Thanks cait
		
		public static Customize FromBytes(byte[] bytes) {
			if (bytes.Length != Customize.Length)
				throw new Exception($"Unexpected length for customize array: {bytes.Length} != {Customize.Length}");
			
			var custom = new Customize();
			for (var i = 0; i < Customize.Length; i++)
				custom.Bytes[i] = bytes[i];
			return custom;
		}
	}

	[Flags]
	public enum FacialFeature : byte {
		None    = 0x00,
		First   = 0x01,
		Second  = 0x02,
		Third   = 0x04,
		Fourth  = 0x08,
		Fifth   = 0x10,
		Sixth   = 0x20,
		Seventh = 0x40,
		Legacy  = 0x80
	}

	public enum Gender : byte {
		Masculine,
		Feminine
	}

	public enum Race : byte {
		Hyur = 1,
		Elezen = 2,
		Lalafell = 3,
		Miqote = 4,
		Roegadyn = 5,
		AuRa = 6,
		Hrothgar = 7,
		Viera = 8
	}

	public enum Tribe : byte {
		Midlander = 1,
		Highlander = 2,
		Wildwood = 3,
		Duskwight = 4,
		Plainsfolk = 5,
		Dunesfolk = 6,
		SunSeeker = 7,
		MoonKeeper = 8,
		SeaWolf = 9,
		Hellsguard = 10,
		Raen = 11,
		Xaela = 12,
		Helion = 13,
		Lost = 14,
		Rava = 15,
		Veena = 16
	}

public enum Age : byte {
		Normal = 1,
		Old = 3,
		Young = 4
	}
}
