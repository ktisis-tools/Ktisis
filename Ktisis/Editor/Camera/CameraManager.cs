using System;
using System.Linq;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Objects.Types;

using GameCameraManager = FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager;

using Ktisis.Interop.Hooking;
using Ktisis.Editor.Camera.Types;
using Ktisis.Editor.Context.Types;

namespace Ktisis.Editor.Camera;

public interface ICameraManager : IDisposable {
	public bool IsValid { get; }
	
	public void Initialize();

	public EditorCamera? Current { get; }
	public IEnumerable<EditorCamera> GetCameras();

	public void SetCurrent(EditorCamera camera);
	public void SetNext();
	public void SetPrevious();
	
	public bool IsWorkCameraActive { get; }
	public void SetWorkCameraMode(bool enabled);
	public void ToggleWorkCameraMode();

	public KtisisCamera Create(CameraFlags flags = CameraFlags.None);
	public bool DeleteCurrent();

	public IGameObject? ResolveOrbitTarget(EditorCamera camera);
}

public class CameraManager : ICameraManager {
	private readonly IEditorContext _context;
	private readonly HookScope _scope;

	public bool IsValid => this._context.IsValid;
	
	public CameraManager(
		IEditorContext context,
		HookScope scope
	) {
		this._context = context;
		this._scope = scope;
	}
	
	// Initialization
	
	private CameraModule? Module { get; set; }

	public void Initialize() {
		Ktisis.Log.Verbose("Initializing camera manager...");
		try {
			this.SetupCameras();
			this.Module = this._scope.Create<CameraModule>(this);
			if (this.Module.Initialize())
				this.Module.Setup();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to initialize camera manager:\n{err}");
		}
	}
	
	private unsafe void SetupCameras() {
		var gameCamera = GameCameraManager.Instance()->GetActiveCamera();
		if (gameCamera == null) return;

		this.Active = this.Default = new EditorCamera(this) {
			Name = "Main Camera",
			Address = (nint)gameCamera,
			Flags = CameraFlags.DefaultCamera
		};
		this.CameraList.Add(this.Default);
	}

	private void SetupWorkCamera() {
		this.WorkCamera ??= new WorkCamera(this, this._context) { Name = "Work Camera" };
		if (!this.CopyOntoCamera(this.WorkCamera))
			throw new Exception("Failed to setup work camera.");
	}
	
	// Camera state
	
	private readonly List<EditorCamera> CameraList = new();

	private EditorCamera? Active { get; set; }
	private EditorCamera? Default { get; set; }
	private WorkCamera? WorkCamera { get; set; }
	
	public bool IsWorkCameraActive { get; private set; }

	public EditorCamera? Current {
		get {
			if (this.IsWorkCameraActive && this.WorkCamera is { IsValid: true } freeCam)
				return freeCam;
			if (this.Active is { IsValid: true } activeCam)
				return activeCam;
			if (this.Default is { IsValid: true } defaultCam)
				return defaultCam;
			return null;
		}	
	}
	
	public IEnumerable<EditorCamera> GetCameras() => this.CameraList;
	
	// Camera management

	public void SetCurrent(EditorCamera camera) {
		if (!camera.IsValid)
			throw new Exception("Attempting to set invalid camera as current.");
		
		if (this.Active == camera) return;
		
		this.Active = camera;
		this.Module?.ChangeCamera(camera);
		if (camera != this.WorkCamera)
			this.IsWorkCameraActive = false;
	}

	public void SetNext() {
		if (this.Current == null || !this.CameraList.Contains(this.Current)) return;
		var next = (this.CameraList.IndexOf(this.Current) + 1) % this.CameraList.Count;
		this.SetCurrent(this.CameraList[next]);
	}

	public void SetPrevious() {
		if (this.Current == null || !this.CameraList.Contains(this.Current)) return;
		var index = this.CameraList.IndexOf(this.Current);
		var prev = (index > 0 ? index : this.CameraList.Count) - 1;
		if (prev < this.CameraList.Count)
			this.SetCurrent(this.CameraList[prev]);
	}
	
	// Work camera

	public void SetWorkCameraMode(bool enabled) {
		if (this.IsWorkCameraActive == enabled) return;

		if (enabled) {
			this.SetupWorkCamera();
			this.Module?.ChangeCamera(this.WorkCamera!);
			this.IsWorkCameraActive = true;
			return;
		}

		this.IsWorkCameraActive = false;
		if (this.Active is { IsValid: true } newCamera)
			this.Module?.ChangeCamera(newCamera);
	}

	public void ToggleWorkCameraMode()
		=> this.SetWorkCameraMode(!this.IsWorkCameraActive);
	
	// Camera creation

	public KtisisCamera Create(CameraFlags flags = CameraFlags.None) {
		var camera = new KtisisCamera(this) {
			Name = this.GetNextAvailableName(),
			Flags = flags
		};
		
		if (camera.Address == nint.Zero)
			throw new Exception("Failed to allocate camera.");
		
		if (!this.CopyOntoCamera(camera))
			throw new Exception("Failed to setup new camera.");
		
		this.CameraList.Add(camera);
		this.SetCurrent(camera);
		
		return camera;
	}

	public bool DeleteCurrent() {
		if (this.Current is not { IsValid: true } active || active.IsDefault )
			return false;

		try {
			this.SetPrevious();
			this.CameraList.Remove(active);
			if (active is KtisisCamera ktActive)
				ktActive.Dispose();
		} catch (Exception e) {
			Ktisis.Log.Error($"CameraManager.DeleteCurrent: {e}");
			return false;
		}

		return true;
	}

	private unsafe bool CopyOntoCamera(EditorCamera camera) {
		if (this.Current is not { IsValid: true } active || active == camera)
			return false;
		
		camera.OrbitTarget = active.OrbitTarget;
		camera.FixedPosition = active.FixedPosition;
		camera.RelativeOffset = active.RelativeOffset;
		*camera.GameCamera = *active.GameCamera;

		if (camera is WorkCamera freeCam) {
			freeCam.SetInitialPosition(active.GetPosition()!.Value, active.Camera->CalcRotation());
			freeCam.Camera->Zoom = 0.0f; // new behavior, should this be a config toggle?
		}
		else
			camera.Flags = active.Flags & ~CameraFlags.DefaultCamera;

		camera.OrthographicZoom = active.OrthographicZoom;
		
		return true;
	}

	private string GetNextAvailableName() {
		for (var i = this.CameraList.Count + 1; i <= 100; i++) {
			var name = $"Camera #{i}";
			if (this.CameraList.Any(camera => camera.Name == name))
				continue;
			return name;
		}
		return "New Camera";
	}
	
	// Camera helpers

	public IGameObject? ResolveOrbitTarget(EditorCamera camera)
		=> this.Module?.ResolveOrbitTarget(camera);
	
	// Disposal

	private void ResetState() {
		this.Default?.ResetState();
		this.Active = null;
		this.WorkCamera?.Dispose();
		this.WorkCamera = null;
		this.CameraList.ForEach(cam => {
			if (cam is KtisisCamera ktisisCam)
				ktisisCam.Dispose();
		});
		this.CameraList.Clear();
	}

	public void Dispose() {
		try {
			this.Module?.Dispose();
			this.ResetState();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to dispose camera manager!\n{err}");
		}
		GC.SuppressFinalize(this);
	}
}
