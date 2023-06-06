using System.Collections.Generic;
using System.Linq;

using Dalamud.Logging;
using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;

using Ktisis.Events;
using Ktisis.Interop.Hooks;
using Ktisis.Structs.Extensions;

namespace Ktisis.Camera {
	internal static class CameraService {
		// Camera spawning

		private static readonly List<KtisisCamera> Cameras = new();

		internal unsafe static GameCamera* _override;
		internal unsafe static GameCamera* Override {
			get => Freecam.Active && GetFreecam() is KtisisCamera freecam ? freecam.GameCamera : _override;
			set => _override = value;
		}

		internal unsafe static Dictionary<nint, string> GetCameraList() {
			var list = new Dictionary<nint, string>();
			list.Add((nint)Services.Camera->Camera, "Default Camera");
			foreach (var camera in Cameras)
				list.Add(camera.Address, camera.Name);
			return list;
		}

		internal unsafe static KtisisCamera SpawnCamera() {
			var active = Services.Camera->GetActiveCamera();
			
			var camera = KtisisCamera.Spawn(active);
			camera.Name = $"Camera #{Cameras.Count + 2}";
			Cameras.Add(camera);

			var tarLock = GetTargetLock(active);
			if (tarLock != null)
				LockTarget(camera.GameCamera, tarLock.ObjectIndex);
			
			return camera;
		}
		
		// Freecam

		internal static WorkCamera Freecam = new();

		private static KtisisCamera? GetFreecam()
			=> Freecam.Active ? Cameras.FirstOrDefault(item => item.IsFreecam) : null;

		internal unsafe static void ToggleFreecam() {
			var isActive = !Freecam.Active;
			if (isActive) {
				var camera = SpawnCamera();
				camera.Name = "Work Camera";
				camera.IsFreecam = true;
				var activeCam = Services.Camera->GetActiveCamera();
				if (activeCam != null) {
					Freecam.Position = activeCam->CameraBase.SceneCamera.Object.Position;
					Freecam.Rotation = activeCam->GetRotation();
				}
				SetCamera(camera.GameCamera);
			} else {
				var cam = GetFreecam();
				if (cam != null) {
					Cameras.Remove(cam);
					cam.Dispose();
				}
				
				var fallback = _override != null ? _override : Services.Camera->Camera;
				SetCamera(fallback);
			}
			Freecam.SetActive(isActive);
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
			if (camera == null) return;
			var mgr = CameraManager.Instance();
			mgr->CameraArraySpan[0] = &camera->CameraBase.SceneCamera;
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
				if (Freecam.Active) ToggleFreecam();
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