using System.Numerics;
using System.Runtime.InteropServices;

using FFXIVClientStructs.Havok;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using Ktisis.Interop.Hooks;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit)]
	public struct ActorModel {
		[FieldOffset(0)] public Object Object;

		[FieldOffset(0x050)] public hkQsTransformf Transform;
		[FieldOffset(0x050)] public Vector3 Position;
		[FieldOffset(0x060)] public Quaternion Rotation;
		[FieldOffset(0x070)] public Vector3 Scale;

		[FieldOffset(0x0A0)] public unsafe Skeleton* Skeleton;

		[FieldOffset(0x148)] public unsafe Breasts* Bust;

		[FieldOffset(0x274)] public float Height;

		public unsafe void SyncModelSpace(bool refPose = false) {
			if (Skeleton == null) return;

			for (var p = 0; p < Skeleton->PartialSkeletonCount; p++) {
				var partial = Skeleton->PartialSkeletons[p];
				var pose = partial.GetHavokPose(0);
				if (pose == null) continue;
				if (refPose) pose->SetToReferencePose();
				PoseHooks.SyncModelSpaceHook.Original(pose);
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
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct Breasts {
		[FieldOffset(0x50)] public ushort LeftBoob;
		[FieldOffset(0x52)] public ushort RightBoob;
		[FieldOffset(0x68)] public Vector3 Scale;
	}
}