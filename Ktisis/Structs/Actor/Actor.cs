using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
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
		
		[FieldOffset(0x6E8)] public ActorDrawData DrawData;

		[FieldOffset(0x8D6)] public bool IsHatHidden;

		public const int GazeOffset = 0xD00;
		[FieldOffset(GazeOffset + 0x10)] public ActorGaze Gaze;
		
		[FieldOffset(0x1AB8)] public uint ModelId;
		
		[FieldOffset(0x226C)] public float Transparency;

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

		public unsafe ItemEquip GetEquip(EquipIndex index) {
			if (index == EquipIndex.Head && this.IsHatHidden)
				return this.DrawData.Equipment.Head;
			return this.Model != null ? this.Model->GetEquipSlot((int)index) : default;
		}

		public unsafe Customize GetCustomize()
			=> this.Model != null ? this.Model->GetCustomize() ?? default : default;

		public WeaponEquip GetWeaponEquip(EquipSlot slot)
			=> slot == EquipSlot.MainHand ? this.DrawData.MainHand.GetEquip() : this.DrawData.OffHand.GetEquip();
		
		public unsafe void Equip(EquipIndex index, ItemEquip item) {
			if (Methods.ActorChangeEquip == null) return;
			
			fixed (ActorDrawData* ptr = &DrawData) {
				//Methods.ActorChangeEquip(ptr, index, (ItemEquip)0xFFFFFFFF);
				Methods.ActorChangeEquip(ptr, index, &item, true);
			}
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
			
			fixed (ActorDrawData* ptr = &DrawData) {
				Logger.Information($"Setting to {item.Set} {item.Base} {item.Variant} {item.Dye}");
                
				Methods.ActorChangeWeapon(ptr, slot, default, 0, 1, 0, 0);
				Methods.ActorChangeWeapon(ptr, slot, item, 0, 1, 0, 0);
				if (slot == 0)
					this.DrawData.MainHand.SetEquip(item);
				else if (slot == 1)
					this.DrawData.OffHand.SetEquip(item);
			}
		}

		public unsafe void SetGlasses(ushort id) {
			if (Methods.ChangeGlasses == null) return;
			
			fixed (ActorDrawData* ptr = &DrawData)
				Methods.ChangeGlasses(ptr, 0, id);
		}

		// Change customize - no redraw method

		public unsafe bool UpdateCustomize() {
			if (this.Model == null) return false;
			
			var result = false;
			
			var human = (Human*)this.Model;
			if (this.Model->IsHuman())
				result = human->UpdateDrawData((byte*)&this.Model->Customize, true);
			
			fixed (Customize* ptr = &DrawData.Customize)
				return result | ((Human*)Model)->UpdateDrawData((byte*)ptr, true);
		}

		// Apply new customize

		public unsafe void ApplyCustomize(Customize custom) {
			if (this.ModelId != 0) return;
			
			var cur = GetCustomize();

			// Fix UpdateCustomize on Carbuncles & Minions
			if (custom.ModelType == 0)
				custom.ModelType = 1;
			
			if (custom.Race == Race.Viera) {
				// avoid crash when loading invalid ears
				var ears = custom.RaceFeatureType;
				custom.RaceFeatureType = ears switch {
					> 4 => 1,
					0 => 4,
					_ => ears
				};
			}

			var faceHack = cur.FaceType != custom.FaceType;
			DrawData.Customize = custom;
			var redraw = !UpdateCustomize()
				|| faceHack
				|| cur.Tribe != custom.Tribe
				|| cur.Gender != custom.Gender;

			if (redraw) {
				Redraw();
			} else if (cur.BustSize != custom.BustSize && Model != null) {
				Model->ScaleBust();
			}
		}

		// Actor redraw

		public void Redraw() {
			var faceHack = GameObject.ObjectKind == ObjectKind.Pc;
			GameObject.DisableDraw();
			if (faceHack) GameObject.ObjectKind = ObjectKind.BattleNpc;
			GameObject.EnableDraw();
			if (faceHack) GameObject.ObjectKind = ObjectKind.Pc;
		}
		
		// weapons

		public unsafe WeaponModel* GetWeaponModel(WeaponSlot slot) {
			var weapon = slot switch {
				WeaponSlot.MainHand => DrawData.MainHand,
				WeaponSlot.OffHand => DrawData.OffHand,
				WeaponSlot.Prop => DrawData.Prop,
				_ => throw new Exception("shit's fucked")
			};
			
			var model = weapon.Model;
			if (model == null || (model->Flags & 9) == 0)
				return null;
			return model;
		}

		public unsafe Skeleton* GetWeaponSkeleton(WeaponSlot slot) {
			var model = GetWeaponModel(slot);
			return model == null ? null : model->Skeleton;
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
