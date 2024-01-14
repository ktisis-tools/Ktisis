using System;
using System.Numerics;

using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;
using CsVector3 = FFXIVClientStructs.FFXIV.Common.Math.Vector3;

namespace Ktisis.Editor.Camera.Types;

public class EditorCamera {
	private readonly ICameraManager _manager;
	
	public virtual nint Address { get; set; } = nint.Zero;

	public string Name = string.Empty;
	public CameraFlags Flags = CameraFlags.None;

	public ushort? OrbitTarget;
	public Vector3? FixedPosition;
	public Vector3 RelativeOffset = Vector3.Zero;

	public EditorCamera(
		ICameraManager manager
	) {
		this._manager = manager;
	}
	
	public bool IsValid => this._manager.IsValid && this.Address != nint.Zero;

	public bool IsDefault => this.Flags.HasFlag(CameraFlags.DefaultCamera);
	public bool IsNoCollide => this.Flags.HasFlag(CameraFlags.NoCollide);
	
	public unsafe GameCamera* GameCamera => (GameCamera*)this.Address;
	public unsafe Structs.Camera* Camera => (Structs.Camera*)this.Address;
	
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
	}
	
	public unsafe void SetDelimited(bool delimit) {
		var camera = this.Camera;
		if (camera == null) return;

		var max = delimit ? 350.0f : 20.0f;
		camera->DistanceMax = max;
		camera->DistanceMin = delimit ? 0.0f : 1.5f;
		camera->Distance = Math.Clamp(camera->Distance, 0.0f, max);
		camera->YMin = delimit ? 1.5f : 1.25f;
		camera->YMax = delimit ? -1.5f : -1.4f;

		if (delimit)
			this.Flags |= CameraFlags.Delimit;
		else
			this.Flags &= ~CameraFlags.Delimit;
	}
}
