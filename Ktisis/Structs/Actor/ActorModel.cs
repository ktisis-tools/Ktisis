using System.Numerics;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.Havok.Common.Base.Math.QsTransform;

using ModelType = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CharacterBase.ModelType;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit)]
	public struct ActorModel {
		[FieldOffset(0)] public Object Object;

		[FieldOffset(0x050)] public hkQsTransformf Transform;
		[FieldOffset(0x050)] public Vector3 Position;
		[FieldOffset(0x060)] public Quaternion Rotation;
		[FieldOffset(0x070)] public Vector3 Scale;

		[FieldOffset(0x88)] public byte Flags;

		[FieldOffset(0x0A0)] public unsafe Skeleton* Skeleton;
		
		[FieldOffset(0x0D0)] public Attach Attach;
		
		[FieldOffset(0x148)] public unsafe Breasts* Bust;
		
		[FieldOffset(0x2A4)] public float Height;

		[FieldOffset(0x370)] public nint Sklb;

		[FieldOffset(0xA10)] public Customize Customize;

		[FieldOffset(0xA18)] public unsafe fixed ulong DemiEquip[5];
		[FieldOffset(0xA30)] public unsafe fixed ulong HumanEquip[11];
		
		[FieldOffset(0x2E0)] public float WeatherWetness;  // Set to 1.0f when raining and not covered or umbrella'd
		[FieldOffset(0x2E4)] public float SwimmingWetness; // Set to 1.0f when in water
		[FieldOffset(0x2E8)] public float WetnessDepth;    // Set to ~character height in GPose and higher values when swimming or diving.

		private unsafe CharacterBase* AsCharacter() {
			fixed (ActorModel* self = &this)
				return (CharacterBase*)self;
		}

		public unsafe bool IsHuman()
			=> AsCharacter()->GetModelType() == ModelType.Human;

		public unsafe Customize? GetCustomize()
			=> IsHuman() ? this.Customize : null;

		public unsafe ItemEquip GetEquipSlot(int slot) => AsCharacter()->GetModelType() switch {
			ModelType.Human => (ItemEquip)this.HumanEquip[slot],
			ModelType.DemiHuman => slot < 5 ? (ItemEquip)this.DemiEquip[slot] : default,
			_ => default
		};

		public unsafe void SyncModelSpace(bool refPose = false) {
			if (Skeleton == null) return;

			for (var p = 0; p < Skeleton->PartialSkeletonCount; p++) {
				var partial = Skeleton->PartialSkeletons[p];
				var pose = partial.GetHavokPose(0);
				if (pose == null) continue;

				pose->SetFromLocalPose(refPose ? pose->Skeleton->ReferencePose : pose->LocalPose);

				if (p > 0) Skeleton->ParentPartialToRoot(p);
			}

			ScaleBust();
		}

		public unsafe void ScaleBust(bool ifUnscaled = false) {
			if (Bust == null) return;
			if (Skeleton == null) return;

			var partial = Skeleton->PartialSkeletons[0];
			var pose = partial.GetHavokPose(0);
			if (pose == null) return;

			var left = Skeleton->GetBone(0, Bust->LeftBoob).AccessModelSpace();
			if (ifUnscaled) {
				var scale = left->Scale;
				var avg = (scale.X + scale.Y + scale.Z) / 3;
				if (avg < 0.99999 || avg > 1.00001) return;
			}
			left->Scale = Bust->Scale.ToHavok();

			var right = Skeleton->GetBone(0, Bust->RightBoob).AccessModelSpace();
			right->Scale = Bust->Scale.ToHavok();
		}

		public unsafe List<nint> GetChildren() {
			var result = new List<nint>();

			// Iterate linked list of child objects.
			var childPtr = Object.ChildObject;
			var child = childPtr;
			while (child != null) {
				if (child->GetObjectType() == ObjectType.CharacterBase)
					result.Add((nint)child);

				child = child->NextSiblingObject;
				if (child == childPtr)
					break;
			}
			
			return result;
		}

		public unsafe float GetAttachScale() {
			if (Attach.Count == 1 && Attach.Type == 4)
				return Attach.BoneAttach->Scale;
			return 1f;
		}
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct Breasts {
		[FieldOffset(0x50)] public ushort LeftBoob;
		[FieldOffset(0x52)] public ushort RightBoob;
		[FieldOffset(0x68)] public Vector3 Scale;
	}
}
