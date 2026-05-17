using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.FFXIV.Client.Game.Control;

using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;
using SceneCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera;
using GameCameraManager = FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager;
using SceneCameraManager = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CameraManager;

using Ktisis.Editor.Camera.Types;
using Ktisis.Interop.Hooking;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Structs.Camera;
using Ktisis.Structs.Input;

using InputManager = Ktisis.Editor.Actions.Input.InputManager;

namespace Ktisis.Editor.Camera;

public class CameraModule : HookModule {
	private readonly CameraManager Manager;
	
	private readonly ISigScanner _sigScanner;
	private readonly IGameInteropProvider _interop;
	private readonly IObjectTable _objectTable;
	
	public CameraModule(
		IHookMediator hook,
		CameraManager manager,
		ISigScanner sigScanner,
		IGameInteropProvider interop,
	IObjectTable objectTable
	) : base(hook) {
		this.Manager = manager;
		this._sigScanner = sigScanner;
		this._interop = interop;
		this._objectTable = objectTable;
	}
	
	// Initialization

	public override bool Initialize() {
		this.InitVfHook();
		return base.Initialize();
	}
	
	private unsafe void InitVfHook() {
		if (!this._sigScanner.TryGetStaticAddressFromSig("48 8D 05 ?? ?? ?? ?? C7 83 ?? ?? ?? ?? ?? ?? ?? ?? 48 89 03 0F 57 C0 33 C0 48 C7 83 ?? ?? ?? ?? ?? ?? ?? ??", out var address)) {
			Ktisis.Log.Warning("Failed to find signature for CameraTarget hook!");
			return;
		}
		var vf = (nint*)address;
		this.CameraTargetHook = this._interop.HookFromAddress<CameraTargetDelegate>(vf[18], this.CameraTargetDetour);
	}

	public void Setup() {
		if (!this.IsInit) return;
		this.CameraControlHook.Enable();
		this.CameraCollideHook.Enable();
		this.CameraTargetHook?.Enable();
		this.CameraCalculateLookPositionHook.Enable();
	}
	
	// Camera change handler

	public void ChangeCamera(EditorCamera camera) {
		if (!this.IsInit) return;
		
		var redirect = !camera.IsDefault;
		Ktisis.Log.Verbose($"Updating redirect hooks: {redirect}");
		if (redirect) {
			this.ActiveCameraHook.Enable();
			this.CameraEventHook.Enable();
			this.CameraUiHook.Enable();
			this.CameraPreUpdateHook.Enable();
		} else {
			this.ActiveCameraHook.Disable();
			this.CameraEventHook.Disable();
			this.CameraUiHook.Disable();
			this.CameraPreUpdateHook.Disable();
		}

		if (camera is WorkCamera) {
			this.CalcViewMatrixHook.Enable();
			this.UpdateInputHook.Enable();
		} else {
			this.CalcViewMatrixHook.Disable();
			this.UpdateInputHook.Disable();
		}

		SetSceneCamera(camera);
		camera.SetActive();
	}
	
	// Orbit target helpers

	public unsafe IGameObject? ResolveOrbitTarget(EditorCamera camera) {
		if (camera.OrbitTarget != null) {
			var target = this._objectTable[camera.OrbitTarget.Value];
			if (target != null) return target;
		}

		var address = (nint)TargetSystem.Instance()->GPoseTarget;
		return this._objectTable.CreateObjectReference(address);
	}
	
	// Camera methods

	[Signature("E8 ?? ?? ?? ?? 48 8B 17 48 8D 4D E0")]
	private LoadMatrixDelegate _loadMatrix = null!;
	private unsafe delegate Matrix4x4* LoadMatrixDelegate(RenderCameraEx* camera, Matrix4x4* matrix);
	
	// Camera control hooks

	[Signature("E8 ?? ?? ?? ?? 48 83 3D ?? ?? ?? ?? ?? 74 0C", DetourName = nameof(CameraControlDetour))]
	private Hook<CameraControlDelegate> CameraControlHook = null!;
	private delegate nint CameraControlDelegate(nint a1);
	
	private nint CameraControlDetour(nint a1) {
		nint result;
		using (var _ = this.Redirect())
			result = this.CameraControlHook.Original(a1);

		try {
			if (this.Manager.Current is { IsValid: true } camera)
				camera.WritePosition();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to handle camera control:\n{err}");
			this.DisableAll();
		}
		
		return result;
	}
	
	[Signature("8B 41 ?? 85 C0 74 ?? 83 F8 ?? 75 ?? 48 8B 41", DetourName = nameof(CameraPreUpdateDetour))]
	private Hook<CameraPreUpdateDelegate> CameraPreUpdateHook = null!;
	private delegate nint CameraPreUpdateDelegate(nint a1);
	
	private nint CameraPreUpdateDetour(nint a1) {
		using var _ = this.Redirect();
		var result = this.CameraPreUpdateHook.Original(a1);
		return result;
	}
	
	// Work camera hooks

	[Signature("48 89 5C 24 ?? 57 48 81 EC ?? ?? ?? ?? F6 81 ?? ?? ?? ?? ?? 48 8B D9 48 89 B4 24 ?? ?? ?? ??", DetourName = nameof(CalcViewMatrixDetour))]
	private Hook<CalcViewMatrixDelegate> CalcViewMatrixHook = null!;
	private unsafe delegate nint CalcViewMatrixDelegate(SceneCamera* camera);

	private unsafe nint CalcViewMatrixDetour(SceneCamera* camera) {
		var result = this.CalcViewMatrixHook.Original(camera);
		
		try {
			if (this.Manager.Current is WorkCamera freeCam) {
				freeCam.Update();
				var matrix = (Matrix4x4*)&camera->ViewMatrix;
				*matrix = freeCam.CalculateViewMatrix();
				this._loadMatrix(freeCam.Camera->RenderEx, matrix);
			}
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to handle work camera:\n{err}");
		}
		
		return result;
	}

	[Signature("E8 ?? ?? ?? ?? 83 7B 58 00", DetourName = nameof(UpdateInputDetour))]
	private Hook<UpdateInputDelegate> UpdateInputHook = null!;
	private unsafe delegate void UpdateInputDelegate(InputDeviceManager* mgr, nint a2, void* controller, MouseDeviceData* mouseData, KeyboardDeviceData* keyData);

	private unsafe void UpdateInputDetour(InputDeviceManager* mgr, nint a2, void* controller, MouseDeviceData* mouseData, KeyboardDeviceData* keyData) {
		this.UpdateInputHook.Original(mgr, a2, controller, mouseData, keyData);

		try {
			if (this.Manager.Current is WorkCamera workCamera && !InputManager.IsChatInputActive())
				workCamera.UpdateControl(mouseData, keyData);
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to handle work camera input:\n{err}");
		}
	}
	
	// Collision hook

	[Signature("48 8B C4 48 89 58 ?? 48 89 70 ?? 48 89 78 ?? 55 41 56 41 57 48 8D 68 ?? 48 81 EC ?? ?? ?? ?? F3 0F 58 1D", DetourName = nameof(CameraCollideDetour))]
	private Hook<CameraCollideDelegate> CameraCollideHook = null!;
	private unsafe delegate nint CameraCollideDelegate(GameCamera* a1, Vector3* a2, Vector3* a3, float a4, nint a5, float a6);

	private unsafe nint CameraCollideDetour(GameCamera* a1, Vector3* a2, Vector3* a3, float a4, nint a5, float a6) {
		if (this.Manager.Current is { IsNoCollide: true } camera && camera.Camera != null) {
			var max = a4 + 0.001f;
			camera.Camera->DistanceCollide.X = max;
			camera.Camera->DistanceCollide.Y = max;
			return 0;
		}
		return this.CameraCollideHook.Original(a1, a2, a3, a4, a5, a6);
	}
	
	// Camera redirect hooks

	[Signature("E8 ?? ?? ?? ?? 45 32 FF 40 32 F6", DetourName = nameof(ActiveCameraDetour))]
	private Hook<ActiveCameraDelegate> ActiveCameraHook = null!;
	private unsafe delegate GameCamera* ActiveCameraDelegate(nint a1);
	
	private unsafe GameCamera* ActiveCameraDetour(nint a1) {
		if (this.Manager.Current is { IsValid: true } camera)
			return camera.GameCamera;
		return this.ActiveCameraHook.Original(a1);
	}

	[Signature("E8 ?? ?? ?? ?? 0F B6 F8 EB 34", DetourName = nameof(CameraEventDetour))]
	private Hook<CameraEventDelegate> CameraEventHook = null!;
	private delegate char CameraEventDelegate(nint a1, nint a2, int a3);
	
	private char CameraEventDetour(nint a1, nint a2, int a3) {
		using var _ = this.Redirect(a3 == 5);
		return this.CameraEventHook.Original(a1, a2, a3);
	}

	[Signature("E8 ?? ?? ?? ?? 80 BB ?? ?? ?? ?? ?? 74 0D 8B 53 28", DetourName = nameof(CameraUiDetour))]
	private Hook<CameraUiDelegate> CameraUiHook = null!;
	private delegate void CameraUiDelegate(nint a1);
	
	private void CameraUiDetour(nint a1) {
		using var _ = this.Redirect();
		this.CameraUiHook.Original(a1);
	}

	[Signature("4C 8B DC 49 89 5B ?? 49 89 73 ?? 55 57 41 56 49 8D 6B ?? 48 81 EC ?? ?? ?? ?? 45 0F 29 4B", DetourName = nameof(CameraCalculateLookPositionDetour))]
	private Hook<CameraCalculateLookPositionDelegate> CameraCalculateLookPositionHook = null!;

	private unsafe delegate float* CameraCalculateLookPositionDelegate(GameCamera* pointer, float* lookAtVector, float* cameraPosition, char cameraMode); // both float* can be cast to CS Vector3* 
	// Orbit target hooks

	private unsafe float* CameraCalculateLookPositionDetour(GameCamera* pointer, float* targetPosition, float* cameraPosition, char mode) {
		if (this.Manager.Current?.Target.Count > 0) {
			Vector3 pos = this.CalculateAveragePosition(this.Manager.Current.Target);
			ActorEntity actor = (ActorEntity)this.Manager.Current.Target.First().Root;
			switch (this.Manager.Current.Tracking) {
				case TrackingMode.Follow:
					this.Manager.Current?.RelativeOffset = pos - (actor.Actor.Position);
					this.Manager.Current?.RelativeOffset.Y = (pos.Y - actor.Actor.Position.Y) - actor.CsGameObject->CameraOffset.Y;
					break;
				case TrackingMode.Pan:
					targetPosition[0] = pos.X;
					targetPosition[1] = pos.Y;
					targetPosition[2] = pos.Z;
					break;
				case TrackingMode.FollowAndPan:
					var lerp = Vector3.Lerp(actor.Actor.Position, pos, Vector3.Hypot(Vector3.Normalize(actor.Actor.Position with{Y = actor.Actor.Position.Y + actor.CsGameObject->CameraOffset.Y}), Vector3.Normalize(pos)).ToScalar() / float.RootN(2, 2));          
					this.Manager.Current?.RelativeOffset = lerp - actor.Actor.Position;
					this.Manager.Current?.RelativeOffset.Y = 0;
					var diff = (lerp - actor.Actor.Position);
					targetPosition[0] = pos.X - diff.X;
					targetPosition[1] = pos.Y;
					targetPosition[2] = pos.Z - diff.Z;
					break;
				case TrackingMode.None:
					this.Manager.Current?.RelativeOffset = this.ResolveOrbitTarget(this.Manager.Current).Position;
					break;
			}
		}
		return  this.CameraCalculateLookPositionHook!.Original(pointer, targetPosition, cameraPosition, mode);
	}
	/*
	 this is to explain the thinking behind that freaky ass math above
	 for the amount we normailze the two position vectors so they both have length = 1 and then take the hypotenuse of them, which results in a vector of maximum length sqrt2 if the vectors are completely tangential.
	 we use this as a factor to ease in and out, where when the original position is roughly where the actors starting location was, the factor should be close to 0, meaning the camera will behave roughly the same.
	 this gives us a factor that will gradually go towards .5~ but shouldn't exceed it.	
	*/
	private Vector3 CalculateAveragePosition(List<BoneNode> points) {
		Vector3 average = new Vector3();

		foreach (var position in points.Where(p => p.GetTransform() != null)) {
			average += position.CalcTransformWorld()!.Position;
		}
		average /= points.Count(p => p.GetTransform() != null);
		return average;
	}
	
	
	
	private Hook<CameraTargetDelegate>? CameraTargetHook;
	private delegate nint CameraTargetDelegate(nint a1);

	private nint CameraTargetDetour(nint a1) {
		if (this.Manager.Current is { OrbitTarget: ushort id }) {
			var target = this._objectTable.GetObjectAddress(id);
			if (target != nint.Zero)
				return target;
		}
		return this.CameraTargetHook!.Original(a1);
	}
	
	// Camera redirect handler

	private unsafe CameraRedirect Redirect(bool condition = true) {
		var mgr = GameCameraManager.Instance();
		var index = mgr->ActiveCameraIndex;
		var table = (GameCamera**)mgr;

		var redirect = new CameraRedirect(index);
		if (!this.Manager.IsValid || !condition) return redirect;

		var active = this.Manager.Current;
		if (active is not { IsDefault: false, GameCamera: not null })
			return redirect;

		redirect.Value = table[index];
		table[index] = active.GameCamera;
		return redirect;
	}

	private class CameraRedirect(int index) : IDisposable {
		public unsafe GameCamera* Value = null;
		
		public unsafe void Dispose() {
			if (this.Value == null) return;
			var table = (GameCamera**)GameCameraManager.Instance();
			table[index] = this.Value;
			this.Value = null;
		}
	}
	
	// Scene camera helpers
	
	private unsafe static void SetSceneCamera(EditorCamera camera) {
		var mgr = SceneCameraManager.Instance();
		mgr->Cameras[mgr->CameraIndex] = &camera.GameCamera->CameraBase.SceneCamera;
	}

	private unsafe static void ResetSceneCamera() {
		var mgr = SceneCameraManager.Instance();
		var active = GameCameraManager.Instance()->GetActiveCamera();
		mgr->Cameras[mgr->CameraIndex] = &active->CameraBase.SceneCamera;
	}
	
	// Disposal

	public override void Dispose() {
		base.Dispose();
		ResetSceneCamera();
		GC.SuppressFinalize(this);
	}
}
