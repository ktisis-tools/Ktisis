using System;
using System.Runtime.InteropServices;

using Ktisis.Structs.Actor;

namespace Ktisis.Data.Files {
	[StructLayout(LayoutKind.Sequential)]
	public struct FfxivCharaDat {
		public uint Magic; // 0x2013FF14
		public uint Version; // 0x05
		public ulong Checksum;
		public Race Race;
		public Gender Gender;
		public byte Age; // 1
		public byte Height;
		public Tribe Tribe;
		public byte Head;
		public byte Hair;
		public byte HasHighlights;
		public byte SkinTone;
		public byte EyeColorRight;
		public byte HairColor;
		public byte HighlightColor;
		public byte FacialFeatures;
		public byte FacialFeatureColor;
		public byte Eyebrows;
		public byte EyeColorLeft;
		public byte Eyes;
		public byte Nose;
		public byte Jaw;
		public byte Mouth;
		public byte LipColor;
		public byte RaceFeatureSize;
		public byte RaceFeatureType;
		public byte BustSize;
		public byte Facepaint;
		public byte FacepaintColor;
		public byte Voice;
		public uint Timestamp;
		public unsafe fixed byte NoteChars[164];

		public unsafe string Note() {
			fixed (byte* ptr = NoteChars)
				return Marshal.PtrToStringAnsi((IntPtr)ptr) ?? "";
		}
	}
}
