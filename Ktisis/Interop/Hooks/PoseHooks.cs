using System;
using System.Collections.Generic;

using Dalamud.Hooking;

using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok;

using Ktisis.Structs;
using Ktisis.Structs.Actor;
using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;

namespace Ktisis.Interop.Hooks {
	public static class PoseHooks {
		internal delegate ulong SetBoneModelSpaceFfxivDelegate(IntPtr partialSkeleton, ushort boneId, IntPtr transform, bool enableSecondary, bool enablePropagate);
		internal static Hook<SetBoneModelSpaceFfxivDelegate> SetBoneModelSpaceFfxivHook = null!;

		internal delegate IntPtr CalculateBoneModelSpaceDelegate(ref hkaPose pose, int boneIdx);
		internal static Hook<CalculateBoneModelSpaceDelegate> CalculateBoneModelSpaceHook = null!;

		internal unsafe delegate void SyncModelSpaceDelegate(hkaPose* pose);
		internal static Hook<SyncModelSpaceDelegate> SyncModelSpaceHook = null!;

		internal unsafe delegate byte* LookAtIKDelegate(byte* a1, long* a2, long* a3, float a4, long* a5, long* a6);
		internal static Hook<LookAtIKDelegate> LookAtIKHook = null!;

		internal unsafe delegate byte AnimFrozenDelegate(uint* a1, int a2);
		internal static Hook<AnimFrozenDelegate> AnimFrozenHook = null!;

		internal unsafe delegate char LoadSkeletonDelegate(Skeleton* a1, ushort a2, IntPtr a3);
		internal static Hook<LoadSkeletonDelegate> LoadSkeletonHook = null!;

		internal unsafe delegate IntPtr DisableDrawDelegate(Actor* a1);
		internal static Hook<DisableDrawDelegate> DisableDrawHook = null!;

		internal static bool PosingEnabled { get; private set; }

		internal static Dictionary<uint, PoseContainer> PreservedPoses = new();

		internal static unsafe void Init() {
			var setBoneModelSpaceFfxiv = Services.SigScanner.ScanText("48 8B C4 48 89 58 18 55 56 57 41 54 41 55 41 56 41 57 48 81 EC ?? ?? ?? ?? 0F 29 70 B8 0F 29 78 A8 44 0F 29 40 ?? 44 0F 29 48 ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B B1");
			SetBoneModelSpaceFfxivHook = Hook<SetBoneModelSpaceFfxivDelegate>.FromAddress(setBoneModelSpaceFfxiv, SetBoneModelSpaceFfxivDetour);

			var calculateBoneModelSpace = Services.SigScanner.ScanText("40 53 48 83 EC 10 4C 8B 49 28");
			CalculateBoneModelSpaceHook = Hook<CalculateBoneModelSpaceDelegate>.FromAddress(calculateBoneModelSpace, CalculateBoneModelSpaceDetour);

			var syncModelSpace = Services.SigScanner.ScanText("48 83 EC 18 80 79 38 00");
			SyncModelSpaceHook = Hook<SyncModelSpaceDelegate>.FromAddress(syncModelSpace, SyncModelSpaceDetour);

			var lookAtIK = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 80 7C 24 ?? ?? 48 8D 4C 24 ??");
			LookAtIKHook = Hook<LookAtIKDelegate>.FromAddress(lookAtIK, LookAtIKDetour);

			var animFrozen = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 0F B6 F0 84 C0 74 0E");
			AnimFrozenHook = Hook<AnimFrozenDelegate>.FromAddress(animFrozen, AnimFrozenDetour);

			var disableDraw = Services.SigScanner.ScanText("48 89 5C 24 ?? 41 56 48 83 EC 20 48 8B D9 48 8B 0D ?? ?? ?? ??");
			DisableDrawHook = Hook<DisableDrawDelegate>.FromAddress(disableDraw, DisableDrawDetour);

			var loadSkele = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 C1 E5 08");
			LoadSkeletonHook = Hook<LoadSkeletonDelegate>.FromAddress(loadSkele, LoadSkeletonDetour);
		}

		internal static void DisablePosing() {
			PreservedPoses.Clear();
			CalculateBoneModelSpaceHook?.Disable();
			SetBoneModelSpaceFfxivHook?.Disable();
			SyncModelSpaceHook?.Disable();
			LookAtIKHook?.Disable();
			AnimFrozenHook?.Disable();
			DisableDrawHook?.Disable();
			LoadSkeletonHook?.Disable();
			PosingEnabled = false;
		}

		internal static void EnablePosing() {
			CalculateBoneModelSpaceHook?.Enable();
			SetBoneModelSpaceFfxivHook?.Enable();
			SyncModelSpaceHook?.Enable();
			LookAtIKHook?.Enable();
			AnimFrozenHook?.Enable();
			DisableDrawHook?.Enable();
			LoadSkeletonHook?.Enable();
			PosingEnabled = true;
		}

		/// <summary>
		/// Toggles posing mode via hooks.
		/// </summary>
		/// <returns></returns>
		internal static bool TogglePosing() {
			if (PosingEnabled) {
				DisablePosing();
			} else {
				EnablePosing();
			}
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
			if (!Ktisis.IsInGPose && PosingEnabled) {
				DisablePosing();
				SyncModelSpaceHook.Original(pose);
			}
		}

		private static unsafe IntPtr DisableDrawDetour(Actor* a1) {
			PoseContainer? container = null;
			if (a1->Model != null) {
				var isActive = a1->RenderMode == RenderMode.Draw || a1->RenderMode == RenderMode.Unload;

				var skeleton = a1->Model->Skeleton;
				if (isActive && skeleton != null) {
					container = new();
					container.Store(skeleton);
				}
			}

			var exec = DisableDrawHook.Original(a1);
			if (exec == IntPtr.Zero && container != null)
				PreservedPoses[a1->ObjectID] = container;

			return exec;
		}

		public static PoseContainer SavedPose = new();
		private static unsafe char LoadSkeletonDetour(Skeleton* a1, ushort a2, IntPtr a3) {
			var exec = LoadSkeletonHook.Original(a1, a2, a3);

			var partial = a1->PartialSkeletons[a2];
			var pose = partial.GetHavokPose(0);
			if (pose == null) return exec;

			SyncModelSpaceHook.Original(pose);

			// Make sure new partials get parented properly
			if (a2 > 0 && partial.ConnectedBoneIndex > -1) {
				var bone = a1->GetBone(a2, partial.ConnectedBoneIndex);
				var parent = a1->GetBone(0, partial.ConnectedParentBoneIndex);

				var model = bone.AccessModelSpace();
				var initial = *model;
				*model = *parent.AccessModelSpace();

				bone.PropagateChildren(model, initial.Translation.ToVector3(), model->Rotation.ToQuat());
				bone.PropagateChildren(model, model->Translation.ToVector3(), initial.Rotation.ToQuat());
			}

			foreach (var obj in Services.ObjectTable) {
				var actor = (Actor*)obj.Address;
				if (actor->Model == null || actor->Model->Skeleton != a1) continue;

				if (PreservedPoses.TryGetValue(actor->ObjectID, out var backup))
					backup.ApplyToPartial(a1, a2);
			}

			return exec;
		}

		public unsafe static byte* LookAtIKDetour(byte* a1, long* a2, long* a3, float a4, long* a5, long* a6) {
			return (byte*)IntPtr.Zero;
		}

		public unsafe static byte AnimFrozenDetour(uint* a1, int a2) {
			return 1;
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
			AnimFrozenHook.Disable();
			AnimFrozenHook.Dispose();
			DisableDrawHook.Disable();
			DisableDrawHook.Dispose();
			LoadSkeletonHook.Disable();
			LoadSkeletonHook.Dispose();
		}
	}
}