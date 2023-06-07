using System;
using System.Runtime.InteropServices;

using Dalamud.Hooking;
using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Common.Math;
using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;
using SceneCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera;

using Ktisis.Camera;

namespace Ktisis.Interop.Hooks {
	internal static class CameraHooks {
		// GameCamera.Ctor

		internal unsafe delegate GameCamera* GameCamera_Ctor_Delegate(GameCamera* self);
		internal static GameCamera_Ctor_Delegate GameCamera_Ctor = null!;

		// Helper functions for camera hooks

		private unsafe static GameCamera* _restore = null;
		
		private unsafe static void StartOverride() {
			if (CameraService.Override == null || _restore != null) return;
			var ptrs = (GameCamera**)Services.Camera;
			var x = Services.Camera->ActiveCameraIndex;
			_restore = ptrs[x];
			if (_restore != null) ptrs[x] = CameraService.Override;
		}

		private unsafe static void EndOverride() {
			if (_restore == null) return;
			var ptrs = (GameCamera**)Services.Camera;
			ptrs[Services.Camera->ActiveCameraIndex] = _restore;
			_restore = null;
		}

		// Camera control

		private delegate nint ControlDelegate(nint a1);
		private static Hook<ControlDelegate> ControlHook = null!;
		private unsafe static nint ControlDetour(nint a1) {
			StartOverride();
			var exec = ControlHook.Original(a1);
			EndOverride();

			var active = Services.Camera->GetActiveCamera();
			var pos = CameraService.GetForcedPos((nint)active);
			if (pos != null) {
				var camera = &active->CameraBase.SceneCamera;
				var curPos = camera->Object.Position;
				
				var newPos = (Vector3)pos;
				camera->Object.Position = newPos;
				camera->LookAtVector += newPos - curPos;
			}

			return exec;
		}
		
		// Freecam

		private unsafe delegate Matrix4x4* CalcViewMatrixDelegate(SceneCamera* camera);
		private static Hook<CalcViewMatrixDelegate> CalcViewMatrixHook = null!;
		private unsafe static Matrix4x4* CalcViewMatrixDetour(SceneCamera* camera) {
			if (!CameraService.Freecam.Active)
				goto retn;

			var active = Services.Camera->GetActiveCamera();
			if (active != null && camera == &active->CameraBase.SceneCamera) {
				var tarMatrix = &camera->ViewMatrix;
				
				var zoom = *(float*)((nint)active + 0x12C);
				var matrix = CameraService.Freecam.Update(active->FoV * Math.Abs(1 + zoom));

				*tarMatrix = matrix;
				return tarMatrix;
			}

			retn: return CalcViewMatrixHook.Original(camera);
		}
		
		// GetActiveCamera

		private unsafe delegate GameCamera* ActiveCamDelegate(nint a1);
		private static Hook<ActiveCamDelegate> ActiveCamHook = null!;
		private unsafe static GameCamera* GetActiveCamDetour(nint a1) {
			if (Ktisis.IsInGPose && CameraService.Override != null)
				return CameraService.Override;
			return ActiveCamHook.Original(a1);
		}

		// AgentCameraSetting.ReceiveEvent

		private delegate char CameraEventDelegate(nint a1, nint a2, int a3);
		private static Hook<CameraEventDelegate> CameraEventHook = null!;
		private static char CameraEventDetour(nint a1, nint a2, int a3) {
			var ov = a3 == 5;
			if (ov) StartOverride();
			var exec = CameraEventHook.Original(a1, a2, a3);
			if (ov) EndOverride();
			return exec;
		}
		
		// Reads camera values for GPose UI.

		private delegate void CameraUiDelegate(nint a1);
		private static Hook<CameraUiDelegate> CameraUiHook = null!;
		private static void CameraUiDetour(nint a1) {
			StartOverride();
			CameraUiHook.Original(a1);
			EndOverride();
		}
		
		// Gets current target of camera for positioning.
		
		private unsafe delegate nint TargetDelegate(GameCamera* a1);
		private static Hook<TargetDelegate> TargetHook = null!;
		private unsafe static nint TargetDetour(GameCamera* a1) {
			if (CameraService.GetTargetLock(a1) is GameObject tar)
				return tar.Address;
			return TargetHook.Original(a1);
		}
		
		// Enable & disable

		internal static bool Enabled = false;
		internal static void SetEnabled(bool enabled) {
			if (Enabled == enabled) return;
			Enabled = enabled;
			if (Enabled)
				EnableHooks();
			else
				DisableHooks();
		}

		private static void EnableHooks() {
			TargetHook.Enable();
			ControlHook.Enable();
			ActiveCamHook.Enable();
			CameraEventHook.Enable();
			CameraUiHook.Enable();
			CalcViewMatrixHook.Enable();
			Enabled = true;
		}

		private static void DisableHooks() {
			TargetHook.Disable();
			ControlHook.Disable();
			ActiveCamHook.Disable();
			CameraEventHook.Disable();
			CameraUiHook.Disable();
			CalcViewMatrixHook.Disable();
			Enabled = false;
		}

		// Init & dispose
		
		internal unsafe static void Init() {
			// Native methods
			
			var ctorAddr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? EB 03 48 8B C7 45 33 C0 48 89 03");
			GameCamera_Ctor = Marshal.GetDelegateForFunctionPointer<GameCamera_Ctor_Delegate>(ctorAddr);

			// Hooks
			
			var camCtrlAddr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 83 3D ?? ?? ?? ?? ?? 74 0C");
			ControlHook = Hook<ControlDelegate>.FromAddress(camCtrlAddr, ControlDetour);

			var actCamAddr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? F7 80");
			ActiveCamHook = Hook<ActiveCamDelegate>.FromAddress(actCamAddr, GetActiveCamDetour);

			var camEventAddr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 0F B6 F0 EB 34");
			CameraEventHook = Hook<CameraEventDelegate>.FromAddress(camEventAddr, CameraEventDetour);

			var camUiAddr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 80 BB ?? ?? ?? ?? ?? 74 0D 8B 53 28");
			CameraUiHook = Hook<CameraUiDelegate>.FromAddress(camUiAddr, CameraUiDetour);

			var camVf17 = ((nint*)Services.SigScanner.GetStaticAddressFromSig("88 83 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 03 C6 83", 6))[17];
			TargetHook = Hook<TargetDelegate>.FromAddress(camVf17, TargetDetour);

			var viewMxAddr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 33 C0 48 89 83 ?? ?? ?? ?? 48 8B 9C 24");
			CalcViewMatrixHook = Hook<CalcViewMatrixDelegate>.FromAddress(viewMxAddr, CalcViewMatrixDetour);
		}

		internal static void Dispose() {
			DisableHooks();
			ControlHook.Dispose();
			ActiveCamHook.Dispose();
			CameraEventHook.Dispose();
			CameraUiHook.Dispose();
			TargetHook.Dispose();
			CalcViewMatrixHook.Dispose();
		}
	}
}