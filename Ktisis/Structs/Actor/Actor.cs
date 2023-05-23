using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Interop;
using Ktisis.Data.Excel;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit, Size = 0x84A)]
	public struct Actor {
		[FieldOffset(0)] public GameObject GameObject;

		[FieldOffset(0x88)] public byte ObjectID;

		[FieldOffset(0x100)] public unsafe ActorModel* Model;
		[FieldOffset(0x114)] public RenderMode RenderMode;
		[FieldOffset(0x1B4)] public uint ModelId;

		[FieldOffset(0x6E8)] public ActorDrawData DrawData;

		[FieldOffset(0x876)] public bool IsHatHidden;
		[FieldOffset(0x877)] public ActorFlags Flags;

		public const int GazeOffset = 0xC60;
		[FieldOffset(GazeOffset + 0x10)] public ActorGaze Gaze;

		public unsafe string? GetName() {
			fixed (byte* ptr = GameObject.Name)
				return ptr == null ? null : Marshal.PtrToStringUTF8((IntPtr)ptr);
		}

		public string GetNameOr(string fallback) {
			var name = GetName();
			return ((ObjectKind)GameObject.ObjectKind == ObjectKind.Pc && !Ktisis.Configuration.DisplayCharName) || string.IsNullOrEmpty(name) ? fallback : name;
		}

		public string GetNameOrId() => GetNameOr("Actor #" + ObjectID);

		// Targeting

		public unsafe void LookAt(Gaze* tar, GazeControl bodyPart) {
			if (Methods.ActorLookAt == null) return;
			
			fixed (ActorGaze* gaze = &Gaze) {
				Methods.ActorLookAt(
					gaze,
					tar,
					bodyPart,
					IntPtr.Zero
				);
			}
		}

		// Change equipment - no redraw method

		public unsafe void Equip(EquipIndex index, ItemEquip item) {
			if (Methods.ActorChangeEquip == null) return;
			fixed (ActorDrawData* ptr = &DrawData)
				Methods.ActorChangeEquip(ptr, index, item);
		}
		public void Equip(List<(EquipSlot, object)> items) {
			foreach ((EquipSlot slot, object item) in items)
				if (item is ItemEquip equip)
					Equip(Interface.Windows.ActorEdit.EditEquip.SlotToIndex(slot), equip);
				else if (item is WeaponEquip wep)
					Equip((int)slot, wep);
		}

		public unsafe void Equip(int slot, WeaponEquip item) {
			if (Methods.ActorChangeWeapon == null) return;
			fixed (ActorDrawData* ptr = &DrawData)
				Methods.ActorChangeWeapon(ptr, slot, item, 0, 1, 0, 0);
		}

		// Change customize - no redraw method

		public unsafe bool UpdateCustomize() {
			fixed (Customize* custom = &DrawData.Customize)
				return ((Human*)Model)->UpdateDrawData((byte*)custom, true);
		}

		// Apply new customize

		public unsafe void ApplyCustomize(Customize custom) {
			var cur = DrawData.Customize;
			DrawData.Customize = custom;

			// Fix UpdateCustomize on Carbuncles & Minions
			if (DrawData.Customize.ModelType == 0)
				DrawData.Customize.ModelType = 1;

			var faceHack = cur.FaceType != custom.FaceType;
			if (cur.Race != custom.Race
				|| cur.Tribe != custom.Tribe // Eye glitch.
				|| cur.Gender != custom.Gender
				|| cur.FaceType != custom.FaceType // Eye glitch.
			) {
				Redraw(faceHack);
			} else {
				if (DrawData.Customize.Race == Race.Viera) {
					// avoid crash when loading invalid ears
					var ears = DrawData.Customize.RaceFeatureType;
					DrawData.Customize.RaceFeatureType = ears switch {
						> 4 => 1,
						0 => 4,
						_ => ears
					};
				}

				var res = UpdateCustomize();
				if (!res) {
					Logger.Warning("Failed to update character. Forcing redraw.");
					Redraw(faceHack);
				} else if (cur.BustSize != custom.BustSize && Model != null) {
					Model->ScaleBust();
				}
			}
		}

		// Actor redraw

		public void Redraw(bool faceHack = false) {
			faceHack &= GameObject.ObjectKind == (byte)ObjectKind.Pc;
			GameObject.DisableDraw();
			if (faceHack) GameObject.ObjectKind = (byte)ObjectKind.BattleNpc;
			GameObject.EnableDraw();
			if (faceHack) GameObject.ObjectKind = (byte)ObjectKind.Pc;
		}
	}

	public enum RenderMode : uint {
		Draw = 0,
		Unload = 2,
		Load = 4
	}

	[Flags]
	public enum ActorFlags : byte {
		None = 0,
		WeaponsVisible = 1,
		WeaponsDrawn = 2,
		VisorToggle = 8
	}
}
