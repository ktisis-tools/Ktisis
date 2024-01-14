using System;
using System.Numerics;

using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.FFXIV.Client.Game.Control;

using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;
using GameCameraManager = FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager;
using SceneCameraManager = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CameraManager;

using Ktisis.Editor.Camera.Types;
using Ktisis.Interop.Hooking;

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
		if (!this._sigScanner.TryGetStaticAddressFromSig("88 83 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 03 C6 83", out var address, 6))
			return;
		var vf = (nint*)address;
		this.CameraTargetHook = this._interop.HookFromAddress<CameraTargetDelegate>(vf[17], this.CameraTargetDetour);
	}

	public void Setup() {
		if (!this.IsInit) return;
		this.CameraControlHook.Enable();
		this.CameraCollideHook.Enable();
		this.CameraTargetHook?.Enable();
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
		} else {
			this.ActiveCameraHook.Disable();
			this.CameraEventHook.Disable();
			this.CameraUiHook.Disable();
		}

		SetSceneCamera(camera);
	}
	
	// Orbit target helpers

	public unsafe GameObject? ResolveOrbitTarget(EditorCamera camera) {
		if (camera.OrbitTarget != null) {
			var target = this._objectTable[camera.OrbitTarget.Value];
			if (target != null) return target;
		}

		var address = (nint)TargetSystem.Instance()->GPoseTarget;
		return this._objectTable.CreateObjectReference(address);
	}
	
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
	
	// Collision hook

	[Signature("E8 ?? ?? ?? ?? 4C 8D 45 C7 89 83", DetourName = nameof(CameraCollideDetour))]
	private Hook<CameraCollideDelegate> CameraCollideHook = null!;
	private unsafe delegate nint CameraCollideDelegate(GameCamera* a1, Vector3* a2, Vector3* a3, float a4, nint a5, float a6);

	private unsafe nint CameraCollideDetour(GameCamera* a1, Vector3* a2, Vector3* a3, float a4, nint a5, float a6) {
		if (this.Manager.Current is { IsValid: true, IsNoCollide: true } camera && camera.Camera != null) {
			var max = a4 + 0.001f;
			camera.Camera->DistanceCollide.X = max;
			camera.Camera->DistanceCollide.Y = max;
			return 0;
		}
		return this.CameraCollideHook.Original(a1, a2, a3, a4, a5, a6);
	}
	
	// Camera redirect hooks

	[Signature("E8 ?? ?? ?? ?? F7 80", DetourName = nameof(ActiveCameraDetour))]
	private Hook<ActiveCameraDelegate> ActiveCameraHook = null!;
	private unsafe delegate GameCamera* ActiveCameraDelegate(nint a1);
	
	private unsafe GameCamera* ActiveCameraDetour(nint a1) {
		if (this.Manager.Current is { IsValid: true } camera)
			return camera.GameCamera;
		return this.ActiveCameraHook.Original(a1);
	}

	[Signature("E8 ?? ?? ?? ?? 0F B6 F0 EB 34", DetourName = nameof(CameraEventDetour))]
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
	
	// Orbit target hooks
	
	private Hook<CameraTargetDelegate>? CameraTargetHook;
	private delegate nint CameraTargetDelegate(nint a1);

	private nint CameraTargetDetour(nint a1) {
		if (this.Manager.Current is { IsValid: true, OrbitTarget: ushort id }) {
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
		if (active is not { IsValid: true, IsDefault: false } || active.GameCamera == null)
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
		mgr->CameraArraySpan[mgr->CameraIndex] = &camera.GameCamera->CameraBase.SceneCamera;
	}

	private unsafe static void ResetSceneCamera() {
		var mgr = SceneCameraManager.Instance();
		var active = GameCameraManager.Instance()->GetActiveCamera();
		mgr->CameraArraySpan[mgr->CameraIndex] = &active->CameraBase.SceneCamera;
	}
	
	// Disposal

	public override void Dispose() {
		base.Dispose();
		ResetSceneCamera();
		GC.SuppressFinalize(this);
	}
}
