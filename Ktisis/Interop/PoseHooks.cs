using System;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Havok;

namespace Ktisis.Interop {
	public static class PoseHooks {
		private delegate ulong SetBoneModelSpaceFfxivDelegate(IntPtr partialSkeleton, ushort boneId, IntPtr transform, bool enableSecondary, bool enablePropagate);
		private static Hook<SetBoneModelSpaceFfxivDelegate> SetBoneModelSpaceFfxivHook = null!;

		private delegate IntPtr CalculateBoneModelSpaceDelegate(ref HkaPose pose, int boneIdx);
		private static Hook<CalculateBoneModelSpaceDelegate> CalculateBoneModelSpaceHook = null!;
		
		private unsafe delegate void SyncModelSpaceDelegate(HkaPose* pose);
		private static Hook<SyncModelSpaceDelegate> SyncModelSpaceHook = null!;
		
		private unsafe delegate hkQsTransform* AccessBoneModelSpaceDelegate(HkaPose* pose, int boneIdx, int propagate);
		private static AccessBoneModelSpaceDelegate AccessBoneModelSpaceFunc = null!;
		
		private unsafe delegate hkQsTransform* AccessBoneLocalSpaceDelegate(HkaPose* pose, int boneIdx);
		private static AccessBoneLocalSpaceDelegate AccessBoneLocalSpaceFunc = null!;
		
		internal static bool PosingEnabled { get; private set; }
		
		internal static unsafe void Init() {
			var setBoneModelSpaceFfxiv = Dalamud.SigScanner.ScanText("48 8B C4 48 89 58 18 55 56 57 41 54 41 55 41 56 41 57 48 81 EC ?? ?? ?? ?? 0F 29 70 B8 0F 29 78 A8 44 0F 29 40 ?? 44 0F 29 48 ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B B1");
			SetBoneModelSpaceFfxivHook = Hook<SetBoneModelSpaceFfxivDelegate>.FromAddress(setBoneModelSpaceFfxiv, SetBoneModelSpaceFfxivDetour);
			
			var calculateBoneModelSpace = Dalamud.SigScanner.ScanText("40 53 48 83 EC 10 4C 8B 49 28");
			CalculateBoneModelSpaceHook = Hook<CalculateBoneModelSpaceDelegate>.FromAddress(calculateBoneModelSpace, CalculateBoneModelSpaceDetour);

			var syncModelSpace = Dalamud.SigScanner.ScanText("48 83 EC 18 80 79 38 00");
			SyncModelSpaceHook = Hook<SyncModelSpaceDelegate>.FromAddress(syncModelSpace, SyncModelSpaceDetour);

			// Not necessary for posing but useful for attempting bone parenting, check Havok docs
			var accessBoneModelSpace = Dalamud.SigScanner.ScanText("48 8B C4 89 50 10 53 57");
			AccessBoneModelSpaceFunc = Marshal.GetDelegateForFunctionPointer<AccessBoneModelSpaceDelegate>(accessBoneModelSpace);
			
			var accessBoneLocalSpace = Dalamud.SigScanner.ScanText("4C 8B DC 53 55 56 57 41 54 41 56 48 81 EC");
			AccessBoneLocalSpaceFunc = Marshal.GetDelegateForFunctionPointer<AccessBoneLocalSpaceDelegate>(accessBoneLocalSpace);
		}

		internal static void DisablePosing()
		{
			CalculateBoneModelSpaceHook?.Disable();
			SetBoneModelSpaceFfxivHook?.Disable();
			SyncModelSpaceHook?.Disable();
			PosingEnabled = false;
		}

		internal static void EnablePosing()
		{
			CalculateBoneModelSpaceHook?.Enable();
			SetBoneModelSpaceFfxivHook?.Enable();
			SyncModelSpaceHook?.Enable();
			PosingEnabled = true;
		}

		/// <summary>
		/// Toggles posing mode via hooks.
		/// </summary>
		/// <returns></returns>
		internal static bool TogglePosing()
		{
			if (CalculateBoneModelSpaceHook.IsEnabled)
				CalculateBoneModelSpaceHook.Disable();
			else
				CalculateBoneModelSpaceHook.Enable();
			if (SetBoneModelSpaceFfxivHook.IsEnabled)
				SetBoneModelSpaceFfxivHook.Disable();
			else
				SetBoneModelSpaceFfxivHook.Enable();
			if (SyncModelSpaceHook.IsEnabled)
				SyncModelSpaceHook.Disable();
			else
				SyncModelSpaceHook.Enable();
			PosingEnabled = !PosingEnabled;
			return PosingEnabled;
		}
		
		private static ulong SetBoneModelSpaceFfxivDetour(IntPtr partialSkeleton, ushort boneId, IntPtr transform, bool enableSecondary, bool enablePropagate)
		{
			return boneId;
		}
		
		private static unsafe IntPtr CalculateBoneModelSpaceDetour(ref HkaPose pose, int boneIdx)
		{
			// This is expected to return the hkQsTransform at the given index in the pose's ModelSpace transform array.
			return (IntPtr) (pose.Transforms.Handle + boneIdx);
		}

		private static unsafe void SyncModelSpaceDetour(HkaPose* pose)
		{
			
		}
		
		public static unsafe void SyncBone(HkaPose* bonesPose, int index)
		{
			CalculateBoneModelSpaceHook.Original(ref *bonesPose, index);
		}

		public static unsafe hkQsTransform* AccessBoneModelSpace(HkaPose* pose, int index, bool propagate)
		{
			return AccessBoneModelSpaceFunc(pose, index, propagate ? 1 : 0);
		}
		
		public static unsafe hkQsTransform* AccessBoneLocalSpace(HkaPose* pose, int index)
		{
			return AccessBoneLocalSpaceFunc(pose, index);
		}

		internal static void Dispose() {
			SetBoneModelSpaceFfxivHook.Disable();
			SetBoneModelSpaceFfxivHook.Dispose();
			CalculateBoneModelSpaceHook.Disable();
			CalculateBoneModelSpaceHook.Dispose();
			SyncModelSpaceHook.Disable();
			SyncModelSpaceHook.Dispose();
		}
	}
}
