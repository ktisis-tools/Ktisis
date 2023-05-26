using System;
using System.Numerics;

using Dalamud.Hooking;

using SceneCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera;

using Ktisis.Camera;

namespace Ktisis.Interop.Hooks {
	internal static class CameraHooks {
		internal unsafe delegate Matrix4x4* CalcViewMatrixDelegate(SceneCamera* a1);
		internal static Hook<CalcViewMatrixDelegate> CalcViewMatrixHook = null!;
		internal unsafe static Matrix4x4* CalcViewMatrixDetour(SceneCamera* camera) {
			var matrix = CalcViewMatrixHook.Original(camera);
			if (!Ktisis.IsInGPose || !WorkCamera.Active) return matrix;

			var activeCam = Services.Camera->GetActiveCamera();
			if (camera == &activeCam->CameraBase.SceneCamera) {
				var zoom = *(float*)((nint)activeCam + 0x12C);
				*matrix = WorkCamera.Update(activeCam->FoV * Math.Abs(1 + zoom));
			}

			return matrix;
		}

		// Init & dispose
		
		internal unsafe static void Init() {
			var addr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 33 C0 48 89 83 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??");
			CalcViewMatrixHook = Hook<CalcViewMatrixDelegate>.FromAddress(addr, CalcViewMatrixDetour);
			CalcViewMatrixHook.Enable();
		}

		internal static void Dispose() {
			CalcViewMatrixHook.Disable();
			CalcViewMatrixHook.Dispose();
		}
	}
}