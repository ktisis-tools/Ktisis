using System;
using System.Runtime.InteropServices;

using Dalamud.Hooking;

using FFXIVClientStructs.FFXIV.Common.Math;
using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;
using SceneCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera;
using RenderCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Camera;

using Ktisis.Camera;
using Ktisis.Structs.FFXIV;

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

		private delegate bool ControlDelegate(nint a1);
		private static Hook<ControlDelegate> ControlHook = null!;
		private unsafe static bool ControlDetour(nint a1) {
			StartOverride();
			var exec = ControlHook.Original(a1);
			EndOverride();

			try {
				var active = Services.Camera->GetActiveCamera();
				var pos = CameraService.GetForcedPos(active);
				if (pos != null) {
					var camera = &active->CameraBase.SceneCamera;
					var curPos = camera->Object.Position;
					
					var newPos = (Vector3)pos;
					camera->Object.Position = newPos;
					camera->LookAtVector += newPos - curPos;
				}
			} catch (Exception e) {
				Ktisis.Log.Error(e.ToString());
				DisableHooks();
			}

			return exec;
		}
		
		// Pre-update

		private delegate nint PreUpdateDelegate(nint a1);
		private static Hook<PreUpdateDelegate> PreUpdateHook = null!;
		private unsafe static nint PreUpdateDetour(nint a1) {
			StartOverride();
			var exec = PreUpdateHook.Original(a1);
			EndOverride();
			return exec;
		}
		
		// Freecam

		private unsafe delegate Matrix4x4* LoadMatrixDelegate(RenderCamera* camera, Matrix4x4* matrix, int a3, int a4);
		private static LoadMatrixDelegate LoadMatrix = null!;

		private unsafe delegate Matrix4x4* CalcViewMatrixDelegate(SceneCamera* camera);
		private static Hook<CalcViewMatrixDelegate> CalcViewMatrixHook = null!;
		private unsafe static Matrix4x4* CalcViewMatrixDetour(SceneCamera* camera) {
			var exec = CalcViewMatrixHook.Original(camera);
			
			var freecam = CameraService.GetFreecam();
			if (freecam == null || freecam.WorkCamera == null)
				return exec;

			try {
				var active = Services.Camera->GetActiveCamera();
				if (active != null && camera == &active->CameraBase.SceneCamera) {
					var tarMatrix = &camera->ViewMatrix;
					
					var zoom = *(float*)((nint)active + 0x12C);
					var matrix = freecam.WorkCamera.Update(active->FoV * Math.Abs(1 + zoom));
	
					*tarMatrix = matrix;
					LoadMatrix(camera->RenderCamera, tarMatrix, 0, 0);
				}
			} catch (Exception e) {
				Ktisis.Log.Error(e.ToString());
				DisableHooks();
			}

			return exec;
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
			if (CameraService.GetTargetLock((nint)a1) is { Address: not 0 } tar)
				return tar.Address;
			return TargetHook.Original(a1);
		}
		
		// Camera collision

		private unsafe delegate nint CameraCollisionDelegate(GPoseCamera* a1, Vector3* a2, Vector3* a3, float a4, nint a5, float a6);
		private static Hook<CameraCollisionDelegate> CameraCollisionHook = null!;
		private unsafe static nint CameraCollisionDetour(GPoseCamera* a1, Vector3* a2, Vector3* a3, float a4, nint a5, float a6) {
			if (Ktisis.IsInGPose && CameraService.GetCameraByAddress((nint)a1) is {CameraEdit.NoClip: true}) {
				var max = a4 + 0.001f;
				a1->DistanceCollide.X = max;
				a1->DistanceCollide.Y = max;
				return 0;
			}
			return CameraCollisionHook.Original(a1, a2, a3, a4, a5, a6);
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
			PreUpdateHook.Enable();
			ActiveCamHook.Enable();
			CameraEventHook.Enable();
			CameraUiHook.Enable();
			CalcViewMatrixHook.Enable();
			CameraCollisionHook.Enable();
			Enabled = true;
		}

		private static void DisableHooks() {
			TargetHook.Disable();
			ControlHook.Disable();
			PreUpdateHook.Disable();
			ActiveCamHook.Disable();
			CameraEventHook.Disable();
			CameraUiHook.Disable();
			CalcViewMatrixHook.Disable();
			CameraCollisionHook.Disable();
			Enabled = false;
		}

		// Init & dispose
		
		internal unsafe static void Init() {
			// Native methods
			
			var ctorAddr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 89 B3 ?? ?? ?? ??");
			GameCamera_Ctor = Marshal.GetDelegateForFunctionPointer<GameCamera_Ctor_Delegate>(ctorAddr);

			// Hooks
			
			var camCtrlAddr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 83 3D ?? ?? ?? ?? ?? 74 0C");
            ControlHook = Services.Hooking.HookFromAddress<ControlDelegate>(camCtrlAddr, ControlDetour);

			var preUpdateAddr = Services.SigScanner.ScanText("48 83 EC 28 8B 41 48");
			PreUpdateHook = Services.Hooking.HookFromAddress<PreUpdateDelegate>(preUpdateAddr, PreUpdateDetour);
            
			var actCamAddr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 45 32 FF 40 32 F6");
            ActiveCamHook = Services.Hooking.HookFromAddress<ActiveCamDelegate>(actCamAddr, GetActiveCamDetour);
            
			var camEventAddr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 0F B6 F8 EB 34");
            CameraEventHook = Services.Hooking.HookFromAddress<CameraEventDelegate>(camEventAddr, CameraEventDetour);
            
			var camUiAddr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 80 BB ?? ?? ?? ?? ?? 74 0D 8B 53 28");
            CameraUiHook = Services.Hooking.HookFromAddress<CameraUiDelegate>(camUiAddr, CameraUiDetour);
            
			var camVf17 = ((nint*)Services.SigScanner.GetStaticAddressFromSig("48 8D 05 ?? ?? ?? ?? C7 83 ?? ?? ?? ?? ?? ?? ?? ?? 48 89 03 0F 57 C0 33 C0 48 C7 83 ?? ?? ?? ?? ?? ?? ?? ??"))[17];
            TargetHook = Services.Hooking.HookFromAddress<TargetDelegate>(camVf17, TargetDetour);
            
			var viewMxAddr = Services.SigScanner.ScanText("48 89 5C 24 ?? 57 48 81 EC ?? ?? ?? ?? F6 81 ?? ?? ?? ?? ?? 48 8B D9 48 89 B4 24 ?? ?? ?? ??");
            CalcViewMatrixHook = Services.Hooking.HookFromAddress<CalcViewMatrixDelegate>(viewMxAddr, CalcViewMatrixDetour);

			var loadMxAddr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B 17 48 8D 4D E0");
			LoadMatrix = Marshal.GetDelegateForFunctionPointer<LoadMatrixDelegate>(loadMxAddr);
            
			var collideAddr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 4C 8B AC 24 ?? ?? ?? ?? 32 DB");
            CameraCollisionHook = Services.Hooking.HookFromAddress<CameraCollisionDelegate>(collideAddr, CameraCollisionDetour);
        }

		internal static void Dispose() {
			DisableHooks();
			ControlHook.Dispose();
			PreUpdateHook.Dispose();
			ActiveCamHook.Dispose();
			CameraEventHook.Dispose();
			CameraUiHook.Dispose();
			TargetHook.Dispose();
			CalcViewMatrixHook.Dispose();
			CameraCollisionHook.Dispose();
		}
	}
}
