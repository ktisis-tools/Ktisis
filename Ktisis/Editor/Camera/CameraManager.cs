using System;
using System.Linq;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Objects.Types;

using GameCameraManager = FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager;

using Ktisis.Interop.Hooking;
using Ktisis.Editor.Context;
using Ktisis.Editor.Camera.Types;

namespace Ktisis.Editor.Camera;

public interface ICameraManager : IDisposable {
	public bool IsValid { get; }
	
	public void Initialize();

	public EditorCamera? Current { get; }
	public IEnumerable<EditorCamera> GetCameras();

	public void SetCurrent(EditorCamera camera);

	public KtisisCamera Create();

	public GameObject? ResolveOrbitTarget(EditorCamera camera);
}

public class CameraManager : ICameraManager {
	private readonly IContextMediator _mediator;
	private readonly HookScope _scope;

	public bool IsValid => this._mediator.Context.IsValid;
	
	public CameraManager(
		IContextMediator mediator,
		HookScope scope
	) {
		this._mediator = mediator;
		this._scope = scope;
	}
	
	// Initialization
	
	private CameraModule? Module { get; set; }

	public void Initialize() {
		this.SetupMainCamera();
		
		this.Module = this._scope.Create<CameraModule>(this);
		if (this.Module.Initialize())
			this.Module.Setup();
	}
	
	private unsafe void SetupMainCamera() {
		var gameCamera = GameCameraManager.Instance()->GetActiveCamera();
		if (gameCamera == null) return;

		this.Current = this.Default = new EditorCamera(this) {
			Name = "Main Camera",
			Address = (nint)gameCamera,
			Flags = CameraFlags.DefaultCamera
		};

		this.Cameras.Add(this.Default);
	}
	
	// Camera state
	
	public EditorCamera? Default { get; private set; }
	public EditorCamera? Current { get; private set; }
	
	private readonly List<EditorCamera> Cameras = new();
	
	public IEnumerable<EditorCamera> GetCameras() => this.Cameras;
	
	// Camera management

	public void SetCurrent(EditorCamera camera) {
		if (!camera.IsValid)
			throw new Exception("Attempting to set invalid camera as current.");
		this.Current = camera;
		this.Module?.ChangeCamera(camera);
	}
	
	// Camera creation

	public KtisisCamera Create() {
		var camera = new KtisisCamera(this) {
			Name = this.GetNextAvailableName()
		};
		
		if (camera.Address == nint.Zero)
			throw new Exception("Failed to allocate camera.");
		if (!this.CopyOntoCamera(camera))
			throw new Exception("Failed to setup spawned camera.");
		
		this.Cameras.Add(camera);
		this.SetCurrent(camera);
		return camera;
	}

	private unsafe bool CopyOntoCamera(EditorCamera camera) {
		if (this.Current is not { IsValid: true } active || active == camera)
			return false;
		camera.OrbitTarget = active.OrbitTarget;
		camera.FixedPosition = active.FixedPosition;
		camera.RelativeOffset = active.RelativeOffset;
		*camera.GameCamera = *active.GameCamera;
		return true;
	}

	private string GetNextAvailableName() {
		for (var i = this.Cameras.Count + 1; i <= 100; i++) {
			var name = $"Camera #{i}";
			if (this.Cameras.Any(camera => camera.Name == name))
				continue;
			return name;
		}
		return "New Camera";
	}
	
	// Camera helpers

	public GameObject? ResolveOrbitTarget(EditorCamera camera)
		=> this.Module?.ResolveOrbitTarget(camera);
	
	// Disposal

	public void Dispose() {
		this.Module?.Dispose();
		this.Default?.SetDelimited(false);
		this.Cameras.Clear();
		this.Current = null;
		GC.SuppressFinalize(this);
	}
}
