using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;

using Ktisis.Events;
using Ktisis.Interop.Hooks;
using Ktisis.Structs.FFXIV;

namespace Ktisis.Camera {
	internal static class CameraService {
		// Camera override
		
		internal unsafe static GameCamera* Override = null;
		
		// Camera list
		
		private static readonly List<KtisisCamera> Cameras = new();

		internal unsafe static KtisisCamera? GetActiveCamera() {
			var active = Services.Camera->GetActiveCamera();

			var camera = GetCameraByAddress((nint)active);
			if (camera == null && Override != null) {
				Ktisis.Log.Warning("Lost track of active camera! Attempting to reset.");
				Reset();
				camera = GetCameraByAddress((nint)Services.Camera->Camera);
			}

			return camera;
		}
		
		internal static KtisisCamera? GetCameraByAddress(nint addr)
			=> Cameras.FirstOrDefault(cam => cam?.Address == addr, null);

		internal static ReadOnlyCollection<KtisisCamera> GetCameraList()
			=> Cameras.AsReadOnly();

		// Camera spawning
		
		internal unsafe static KtisisCamera SpawnCamera(bool cloneEdits = true) {
			var active = GetActiveCamera();
			
			var camera = KtisisCamera.Spawn(active != null ? active.GameCamera : Services.Camera->Camera);
			camera.Name = $"Camera #{Cameras.Count + 1}";
			Cameras.Add(camera);

			if (active != null && cloneEdits)
				camera.CameraEdit = active.CameraEdit.Clone();

			return camera;
		}

		internal unsafe static void RemoveCamera(KtisisCamera cam) {
			if (Override == cam.GameCamera) {
				Override = null;
				Reset();
			}
			Cameras.Remove(cam);
			cam.Dispose();
		}

		// Camera edits

		internal unsafe static Vector3? GetForcedPos(GameCamera* addr) {
			var active = GetActiveCamera();
			if (active == null) return null;
			
			var pos = active.WorkCamera?.InterpPos ?? active.CameraEdit.Position;
			if (active.CameraEdit.Offset != null) {
				if (pos == null)
					pos = addr == null ? default : addr->CameraBase.SceneCamera.Object.Position;
				pos = pos + active.CameraEdit.Offset;
			}

			return pos;
		}
		
		internal static IGameObject? GetTargetLock(nint addr) {
			if (!Ktisis.IsInGPose || GetCameraByAddress(addr) is not KtisisCamera camera)
				return null;

			return camera.CameraEdit.Orbit != null ? Services.ObjectTable.FirstOrDefault(
				actor => actor.ObjectIndex == camera.CameraEdit.Orbit
			) : null;
		}
		
		// Freecam

		internal static KtisisCamera? GetFreecam() {
			var active = GetActiveCamera();
			return active?.WorkCamera != null ? active : null;
		}

		internal unsafe static void ToggleFreecam() {
			var active = GetActiveCamera();
			if (active?.WorkCamera is WorkCamera) {
				RemoveCamera(active);
				if (GetCameraByAddress(active.ClonedFrom) is KtisisCamera clonedFrom && clonedFrom.GameCamera != null)
					SetOverride(clonedFrom);
				else
					Reset();
			} else {
				var camera = Cameras.FirstOrDefault(
					cam => cam.WorkCamera != null,
					SpawnCamera(false)
				);
				camera.Name = "Work Camera";
				camera.AsGPoseCamera()->FoV = 0;
				var workCam = new WorkCamera();
				if (active != null) {
					workCam.Position = active.Position;
					workCam.Rotation = active.Rotation;
				}
				camera.WorkCamera = workCam;
				workCam.SetActive(true);
				SetOverride(camera);
			}
		}

		private static void CheckFreecam(KtisisCamera? other = null) {
			if (GetFreecam() is KtisisCamera freecam && freecam != other)
				RemoveCamera(freecam);
		}

		// CameraManager wrappers

		internal unsafe static void Reset() {
			Ktisis.Log.Debug("Resetting camera");
			CheckFreecam();
			Override = null;
			SetCamera(Services.Camera->Camera);
		}

		private unsafe static void SetCamera(GameCamera* camera) {
			if (camera == null) return;
			var mgr = CameraManager.Instance();
			mgr->Cameras[0] = &camera->CameraBase.SceneCamera;
		}
		
		// Overrides
		
		internal unsafe static void SetOverride(KtisisCamera camera) {
			Ktisis.Log.Debug($"Setting camera to {camera.Name}");
			
			CheckFreecam(camera);
			
			if (camera.IsNative || !camera.IsValid()) {
				Reset();
				return;
			}
			
			Override = camera.GameCamera;
			if (Override != null) SetCamera(Override);
		}

		// Init & Dispose
		
		internal static void Init() {
			CameraHooks.Init();
			EventManager.OnGPoseChange += OnGPoseChange;
		}
		
		internal static void ChangeCameraIndex(int offset) {
			var camera = GetActiveCamera();
			if (camera == null) 
				return;
				
			if (GetFreecam() == camera)
				return;

			var newIndex = (Cameras.FindIndex(tofind => tofind == camera) + offset) % Cameras.Count;
			if (newIndex < 0) // loop back to start.
				newIndex = Cameras.Count - 1; 
				
			SetOverride(Cameras[newIndex]);
		}

		internal static void Dispose() {
			EventManager.OnGPoseChange -= OnGPoseChange;
			CameraHooks.Dispose();
			DisposeCameras();
		}
		
		// Events

		private unsafe static void OnGPoseChange(bool state) {
			if (state) PrepareCameraList();
			CameraHooks.SetEnabled(state);
			if (!state) {
				DisposeCameras();

				var active = (GPoseCamera*)Services.Camera->GetActiveCamera();
				if (active != null) {
					active->DistanceMax = 20;
					active->DistanceMin = 1.5f;
					active->YMax = -1.5f;
					active->YMin = 1.5f;
					active->Distance = Math.Clamp(active->Distance, 0, 20);
				}
			}
		}

		private unsafe static void PrepareCameraList() {
			Cameras.Clear();
			
			var addr = Services.Camera->Camera;
			if (addr == null) return;

			var camera = KtisisCamera.Native(addr);
			camera.Name = "Default Camera";
			Cameras.Add(camera);
		}

		private unsafe static void DisposeCameras() {
			Ktisis.Log.Debug("Disposing cameras...");
			if (Override != null)
				Reset();
			foreach (var cam in Cameras)
				cam.Dispose();
			Cameras.Clear();
		}
	}
}
