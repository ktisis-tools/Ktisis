using System;
using System.Numerics;

using Ktisis.Structs.Camera;

using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;
using CsVector3 = FFXIVClientStructs.FFXIV.Common.Math.Vector3;

namespace Ktisis.Editor.Camera.Types;

public class EditorCamera {
	protected readonly ICameraManager Manager;
	
	public virtual nint Address { get; set; } = nint.Zero;

	public string Name = string.Empty;
	public CameraFlags Flags = CameraFlags.None;

	public ushort? OrbitTarget;
	public Vector3? FixedPosition;
	public Vector3 RelativeOffset = Vector3.Zero;

	public float OrthographicZoom = 10.0f;

	public EditorCamera(
		ICameraManager manager
	) {
		this.Manager = manager;
	}
	
	public bool IsValid => this.Manager.IsValid && this.Address != nint.Zero;

	public bool IsDefault => this.Flags.HasFlag(CameraFlags.DefaultCamera);
	public bool IsNoCollide => this.Flags.HasFlag(CameraFlags.NoCollide);
	public bool IsOrthographic => this.Flags.HasFlag(CameraFlags.Orthographic);
	public bool IsDelimited => this.Flags.HasFlag(CameraFlags.Delimit);
	
	public unsafe GameCamera* GameCamera => (GameCamera*)this.Address;
	public unsafe GameCameraEx* Camera => (GameCameraEx*)this.Address;

	public void SetActive() {
		this.SetOrthographic(this.IsOrthographic);
	}
	
	public unsafe virtual Vector3? GetPosition() {
		var camera = this.GameCamera;
		if (camera == null) return null;
		return this.FixedPosition ?? camera->CameraBase.SceneCamera.Object.Position;
	}

	public unsafe virtual void WritePosition() {
		var camera = this.GameCamera;
		if (camera == null) return;

		var sceneCam = &camera->CameraBase.SceneCamera;
		var curPos = sceneCam->Object.Position;

		var newPos = (CsVector3)(this.GetPosition()!.Value + this.RelativeOffset);
		sceneCam->Object.Position = newPos;
		sceneCam->LookAtVector += newPos - curPos;

		var renderCam = this.Camera->RenderEx;
		if (renderCam != null && this.IsOrthographic)
			renderCam->OrthographicZoom = this.OrthographicZoom;
	}
	
	public unsafe void SetDelimited(bool delimit) {
		if (delimit)
			this.Flags |= CameraFlags.Delimit;
		else
			this.Flags &= ~CameraFlags.Delimit;
        
		var camera = this.Camera;
		if (camera == null) return;

		var max = delimit ? 350.0f : 20.0f;
		camera->DistanceMax = max;
		camera->DistanceMin = delimit ? 0.0f : 1.5f;
		camera->Distance = Math.Clamp(camera->Distance, 0.0f, max);
		camera->YMin = delimit ? 1.5f : 1.25f;
		camera->YMax = delimit ? -1.5f : -1.4f;
	}

	public unsafe void SetOrthographic(bool enabled) {
		if (enabled)
			this.Flags |= CameraFlags.Orthographic;
		else
			this.Flags &= ~CameraFlags.Orthographic;
        
		var render = this.Camera->RenderEx;
		if (render == null) return;
		
		render->OrthographicEnabled = enabled;
		render->OrthographicZoom = enabled ? this.OrthographicZoom : 10.0f;
	}

	public unsafe void ResetState() {
		if (this.Camera == null) return;
		this.SetDelimited(false);
		this.SetOrthographic(false);
	}
}
