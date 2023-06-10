using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using Dalamud.Logging;
using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;

using Ktisis.Events;
using Ktisis.Interop.Hooks;
using Ktisis.Structs.FFXIV;
using Ktisis.Structs.Extensions;

namespace Ktisis.Camera {
	internal static class CameraService {
		// Camera spawning
		
		internal unsafe static GameCamera* _override;
		internal unsafe static GameCamera* Override {
			get => Freecam.Active && GetFreecam() is KtisisCamera freecam ? freecam.GameCamera : _override;
			set => _override = value;
		}
		
		private static readonly List<KtisisCamera> Cameras = new();
		
		internal unsafe static KtisisCamera SpawnCamera() {
			var active = Services.Camera->GetActiveCamera();
			
			var camera = KtisisCamera.Spawn(active);
			camera.Name = $"Camera #{Cameras.Count + 2}";
			Cameras.Add(camera);

			var edit = GetCameraEdit((nint)active);
			if (edit != null)
				CameraEdits.Add(camera.Address, edit.Clone());

			return camera;
		}

		internal static void RemoveCamera(KtisisCamera cam) {
			Cameras.Remove(cam);
            CameraEdits.Remove(cam.Address);
            cam.Dispose();
		}
		
		internal unsafe static Dictionary<nint, string> GetCameraList() {
			var list = new Dictionary<nint, string>();
			list.Add((nint)Services.Camera->Camera, "Default Camera");
			foreach (var camera in Cameras)
				list.Add(camera.Address, camera.Name);
			return list;
		}
		
		// Camera edits
        
		private static Dictionary<nint, CameraEdit> CameraEdits = new();
        
		internal static CameraEdit? GetCameraEdit(nint addr)
			=> CameraEdits.GetValueOrDefault(addr);
        
		internal static CameraEdit GetCameraEditOrNew(nint addr) {
			var result = GetCameraEdit(addr);
			if (result == null) {
				result = new CameraEdit();
				CameraEdits.Add(addr, result);
			}
			return result;
		}
        
		internal unsafe static Vector3? GetForcedPos(GameCamera* addr) {
			var edit = GetCameraEdit((nint)addr);
			var pos = Freecam.Active ? Freecam.InterpPos : edit?.Position;
			if (edit?.Offset != null) {
				if (pos == null)
					pos = addr->CameraBase.SceneCamera.Object.Position;
				pos += edit.Offset;
			}
			return pos;
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
				((GPoseCamera*)camera.GameCamera)->FoV = 0;
				var activeCam = Services.Camera->GetActiveCamera();
				if (activeCam != null) {
					Freecam.Position = activeCam->CameraBase.SceneCamera.Object.Position;
					Freecam.Rotation = activeCam->GetRotation();
				}
				SetCamera(camera.GameCamera);
			} else {
				var cam = GetFreecam();
				if (cam != null)
					RemoveCamera(cam);

				var fallback = _override != null ? _override : Services.Camera->Camera;
				SetCamera(fallback);
			}
			Freecam.SetActive(isActive);
		}

		// Target lock

		internal static void SetTargetLock(nint addr, ushort? tarId) {
			var edit = GetCameraEditOrNew(addr);
			edit.Orbit = tarId;
			if (tarId == null && edit.IsEmpty())
				CameraEdits.Remove(addr);
			else
				CameraEdits[addr] = edit;
		}

		internal static GameObject? GetTargetLock(nint addr) {
			if (!Ktisis.IsInGPose || GetCameraEdit(addr) is not CameraEdit edit)
				return null;
			return edit.Orbit != null ? Services.ObjectTable.FirstOrDefault(
				actor => actor.ObjectIndex == edit.Orbit
			) : null;
		}

		// Position lock

		internal static void SetPositionLock(nint addr, Vector3? pos) {
			var edit = GetCameraEditOrNew(addr);
			edit.Position = pos;
			if (pos == null && edit.IsEmpty())
				CameraEdits.Remove(addr);
			else
				CameraEdits[addr] = edit;
		}

		internal static Vector3? GetPositionLock(nint addr) {
			if (!Ktisis.IsInGPose || GetCameraEdit(addr) is not CameraEdit edit)
				return null;
			return edit.Position;
		}

		// Offset

		internal static void SetOffset(nint addr, Vector3? off) {
			var edit = GetCameraEditOrNew(addr);
			edit.Offset = off;
			if (off == null && edit.IsEmpty())
				CameraEdits.Remove(addr);
			else
				CameraEdits[addr] = edit;
		}

		internal static Vector3? GetOffset(nint addr) {
			if (!Ktisis.IsInGPose || GetCameraEdit(addr) is not CameraEdit edit)
				return null;
			return edit.Offset;
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
				CameraEdits.Clear();
				DisposeCameras();
			}
		}

		private unsafe static void DisposeCameras() {
			PluginLog.Debug("Disposing cameras...");
			if (Override != null)
				Reset();
			foreach (var cam in Cameras)
				cam.Dispose();
			Cameras.Clear();
		}
	}

	public class CameraEdit {
		public ushort? Orbit;
		public Vector3? Position;
		public Vector3? Offset;

		public bool IsEmpty() => GetType().GetFields()
			.All(item => item.GetValue(this) == null);

		public CameraEdit Clone() {
			var result = new CameraEdit();
			foreach (var field in GetType().GetFields())
				field.SetValue(result, field.GetValue(this));
			return result;
		}
	}
}