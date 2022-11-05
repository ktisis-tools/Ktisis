using System;
using Dalamud.Game.ClientState.Objects.Types;

using Dalamud.Hooking;

using FFXIVClientStructs.Havok;

namespace Ktisis.Interop.Hooks {
	public static class PoseHooks {
		private delegate ulong SetBoneModelSpaceFfxivDelegate(IntPtr partialSkeleton, ushort boneId, IntPtr transform, bool enableSecondary, bool enablePropagate);
		private static Hook<SetBoneModelSpaceFfxivDelegate> SetBoneModelSpaceFfxivHook = null!;

		private delegate IntPtr CalculateBoneModelSpaceDelegate(ref hkaPose pose, int boneIdx);
		private static Hook<CalculateBoneModelSpaceDelegate> CalculateBoneModelSpaceHook = null!;

		internal unsafe delegate void SyncModelSpaceDelegate(hkaPose* pose);
		internal static Hook<SyncModelSpaceDelegate> SyncModelSpaceHook = null!;

		private unsafe delegate byte* LookAtIKDelegate(byte* a1, long* a2, long* a3, float a4, long* a5, long* a6);
		private static Hook<LookAtIKDelegate> LookAtIKHook = null!;

		internal static bool PosingEnabled { get; private set; }

		internal static unsafe void Init() {
			var setBoneModelSpaceFfxiv = Dalamud.SigScanner.ScanText("48 8B C4 48 89 58 18 55 56 57 41 54 41 55 41 56 41 57 48 81 EC ?? ?? ?? ?? 0F 29 70 B8 0F 29 78 A8 44 0F 29 40 ?? 44 0F 29 48 ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B B1");
			SetBoneModelSpaceFfxivHook = Hook<SetBoneModelSpaceFfxivDelegate>.FromAddress(setBoneModelSpaceFfxiv, SetBoneModelSpaceFfxivDetour);

			var calculateBoneModelSpace = Dalamud.SigScanner.ScanText("40 53 48 83 EC 10 4C 8B 49 28");
			CalculateBoneModelSpaceHook = Hook<CalculateBoneModelSpaceDelegate>.FromAddress(calculateBoneModelSpace, CalculateBoneModelSpaceDetour);

			var syncModelSpace = Dalamud.SigScanner.ScanText("48 83 EC 18 80 79 38 00");
			SyncModelSpaceHook = Hook<SyncModelSpaceDelegate>.FromAddress(syncModelSpace, SyncModelSpaceDetour);

			var lookAtIK = Dalamud.SigScanner.ScanText("E8 ?? ?? ?? ?? 80 7C 24 ?? ?? 48 8D 4C 24 ??");
			LookAtIKHook = Hook<LookAtIKDelegate>.FromAddress(lookAtIK, LookAtIKDetour);
		}

		internal static void DisablePosing() {
			CalculateBoneModelSpaceHook?.Disable();
			SetBoneModelSpaceFfxivHook?.Disable();
			SyncModelSpaceHook?.Disable();
			LookAtIKHook?.Disable();
			PosingEnabled = false;
		}

		internal static void EnablePosing() {
			CalculateBoneModelSpaceHook?.Enable();
			SetBoneModelSpaceFfxivHook?.Enable();
			SyncModelSpaceHook?.Enable();
			LookAtIKHook?.Enable();
			PosingEnabled = true;
		}

		/// <summary>
		/// Toggles posing mode via hooks.
		/// </summary>
		/// <returns></returns>
		internal static bool TogglePosing() {
			if (PosingEnabled) {
				CalculateBoneModelSpaceHook.Disable();
				SetBoneModelSpaceFfxivHook.Disable();
				SyncModelSpaceHook.Disable();
				LookAtIKHook.Disable();
			} else {
				CalculateBoneModelSpaceHook.Enable();
				SetBoneModelSpaceFfxivHook.Enable();
				SyncModelSpaceHook.Enable();
				LookAtIKHook.Enable();
			}
			PosingEnabled = !PosingEnabled;
			return PosingEnabled;
		}

		private static ulong SetBoneModelSpaceFfxivDetour(IntPtr partialSkeleton, ushort boneId, IntPtr transform, bool enableSecondary, bool enablePropagate) {
			return boneId;
		}

		private static unsafe IntPtr CalculateBoneModelSpaceDetour(ref hkaPose pose, int boneIdx) {
			// This is expected to return the hkQsTransform at the given index in the pose's ModelSpace transform array.
			return (IntPtr)(pose.ModelPose.Data + boneIdx);
		}

		private static unsafe void SyncModelSpaceDetour(hkaPose* pose) {

		}

		public unsafe static byte* LookAtIKDetour(byte* a1, long* a2, long* a3, float a4, long* a5, long* a6) {
			return (byte*)IntPtr.Zero;
		}

		public static unsafe void SyncBone(hkaPose* bonesPose, int index) {
			CalculateBoneModelSpaceHook.Original(ref *bonesPose, index);
		}

		public static unsafe bool IsGamePlaybackRunning(GameObject? gPoseTarget) {
			var animationControl = GetAnimationControl(gPoseTarget);
			if (animationControl == null) return true;
			return animationControl->PlaybackSpeed == 1;
		}

		public static unsafe hkaDefaultAnimationControl* GetAnimationControl(GameObject? go) {
			if (go == null) return null;
			var csObject = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)go.Address;
			if (csObject->DrawObject == null ||
				csObject->DrawObject->Skeleton == null ||
				csObject->DrawObject->Skeleton->PartialSkeletons == null ||
				csObject->DrawObject->Skeleton->PartialSkeletons->GetHavokAnimatedSkeleton(0) == null ||
				csObject->DrawObject->Skeleton->PartialSkeletons->GetHavokAnimatedSkeleton(0)->AnimationControls.Length == 0 ||
				csObject->DrawObject->Skeleton->PartialSkeletons->GetHavokAnimatedSkeleton(0)->AnimationControls[0].Value == null)
				return null;
			return csObject->DrawObject->Skeleton->PartialSkeletons->GetHavokAnimatedSkeleton(0)->AnimationControls[0];
		}

		internal static void Dispose() {
			SetBoneModelSpaceFfxivHook.Disable();
			SetBoneModelSpaceFfxivHook.Dispose();
			CalculateBoneModelSpaceHook.Disable();
			CalculateBoneModelSpaceHook.Dispose();
			SyncModelSpaceHook.Disable();
			SyncModelSpaceHook.Dispose();
			LookAtIKHook.Disable();
			LookAtIKHook.Dispose();
		}
	}
}
