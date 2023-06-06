using System.Collections.Generic;
using System.Linq;

using Dalamud.Logging;
using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;

using Ktisis.Events;
using Ktisis.Interop.Hooks;

namespace Ktisis.Camera {
	internal static class CameraService {
		// Camera spawning

		internal static List<KtisisCamera> Cameras = new();

		internal unsafe static GameCamera* Override = null;
		
		internal unsafe static Dictionary<nint, string> GetCameraList() {
			var list = new Dictionary<nint, string>();
			list.Add((nint)Services.Camera->Camera, "Default Camera");
			foreach (var camera in Cameras)
				list.Add(camera.Address, camera.Name);
			return list;
		}

		internal unsafe static KtisisCamera SpawnCamera() {
			var camera = KtisisCamera.Spawn(Services.Camera->GetActiveCamera());
			Cameras.Add(camera);
			return camera;
		}
		
		// Target locking

		private static Dictionary<nint, ushort> TargetLock = new();

		internal unsafe static void LockTarget(GameCamera* camera, ushort tarId) {
			UnlockTarget(camera);
			TargetLock.Add((nint)camera, tarId);
		}

		internal unsafe static void UnlockTarget(GameCamera* camera) {
			var key = (nint)camera;
			if (TargetLock.ContainsKey(key))
				TargetLock.Remove(key);
		}
		
		internal unsafe static GameObject? GetTargetLock(GameCamera* camera) {
			if (!Ktisis.IsInGPose || !TargetLock.TryGetValue((nint)camera, out var tarId))
				return null;
			return Services.ObjectTable.FirstOrDefault(actor => actor.ObjectIndex == tarId);
		}

		// CameraManager wrappers

		internal unsafe static void Reset() {
			Override = null;
			SetCamera(Services.Camera->Camera);
		}

		private unsafe static void SetCamera(GameCamera* camera) {
			var mgr = CameraManager.Instance();
			mgr->CameraArraySpan[0] = &camera->CameraBase.SceneCamera;
			//CameraHooks.SetCamera(mgr, 0);
		}
		
		// Overrides

		internal unsafe static void SetOverride(GameCamera* camera) {
			Override = camera;
			SetCamera(Override);
		}

		internal unsafe static void SetOverride(nint camera)
			=> SetOverride((GameCamera*)camera);

		// Init & Dispose
		
		internal static void Init() {
			CameraHooks.Init();
			EventManager.OnGPoseChange += OnGPoseChange;
		}

		internal static void Dispose() {
			EventManager.OnGPoseChange -= OnGPoseChange;
			CameraHooks.Dispose();
			DisposeCameras();
		}
		
		// Events

		private static void OnGPoseChange(bool state) {
			CameraHooks.SetEnabled(state);
			if (!state) {
				TargetLock.Clear();
				DisposeCameras();
			}
		}

		private unsafe static void DisposeCameras() {
			PluginLog.Debug("Disposing cameras...");
			foreach (var cam in Cameras)
				cam.Dispose();
			Cameras.Clear();
			if (Override != null)
				Reset();
		}
	}
}