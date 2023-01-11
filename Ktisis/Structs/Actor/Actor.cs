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

		[FieldOffset(0x6E0)] public Weapon MainHand;
		[FieldOffset(0x748)] public Weapon OffHand;
		[FieldOffset(0x818)] public Equipment Equipment;
		[FieldOffset(0x840)] public Customize Customize;

		[FieldOffset(0xC40)] public ActorGaze Gaze; // Update in ActorHooks.cs as well

		public unsafe string? Name => Marshal.PtrToStringAnsi((IntPtr)GameObject.GetName());

		public string GetNameOr(string fallback) => ((ObjectKind)GameObject.ObjectKind == ObjectKind.Pc && !Ktisis.Configuration.DisplayCharName) || string.IsNullOrEmpty(Name)? fallback : Name;
		public string GetNameOrId() => GetNameOr("Actor #" + ObjectID);

		public unsafe IntPtr GetAddress() {
			fixed (Actor* self = &this) return (IntPtr)self;
		}

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

		public void Equip(EquipIndex index, ItemEquip item) {
			if (Methods.ActorChangeEquip == null) return;
			Methods.ActorChangeEquip(GetAddress() + 0x6D0, index, item);
		}
		public void Equip(List<(EquipSlot, object)> items) {
			foreach ((EquipSlot slot, object item) in items)
				if (item is ItemEquip equip)
					Equip(Interface.Windows.ActorEdit.EditEquip.SlotToIndex(slot), equip);
				else if (item is WeaponEquip wep)
					Equip((int)slot, wep);
		}

		public void Equip(int slot, WeaponEquip item) {
			if (Methods.ActorChangeWeapon == null) return;
			Methods.ActorChangeWeapon(GetAddress() + 0x6D0, slot, item, 0, 1, 0, 0);
		}

		// Change customize - no redraw method

		public unsafe bool UpdateCustomize() {
			fixed (Customize* custom = &Customize)
				return ((Human*)Model)->UpdateDrawData((byte*)custom, true);
		}

		// Apply new customize

		public unsafe void ApplyCustomize(Customize custom) {
			var cur = Customize;
			Customize = custom;

			// Fix UpdateCustomize on Carbuncles & Minions
			if (Customize.ModelType == 0)
				Customize.ModelType = 1;

			var faceHack = cur.FaceType != custom.FaceType;
			if (cur.Race != custom.Race
				|| cur.Tribe != custom.Tribe // Eye glitch.
				|| cur.Gender != custom.Gender
				|| cur.FaceType != custom.FaceType // Eye glitch.
			) {
				Redraw(faceHack);
			} else {
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
}
