using System;
using System.Numerics;

using Dalamud.Game.ClientState.Keys;

using Ktisis.Structs.Input;
using Ktisis.Editor.Context.Types;

namespace Ktisis.Editor.Camera.Types;

public class WorkCamera : KtisisCamera {
	private readonly IEditorContext _ctx;
	private readonly static Vector3 UpVector = Vector3.UnitY;
	private const float ClampY = 1.57072f;

	public Vector3 Position;
	public Vector3 Rotation;

	private float MoveSpeed;
	
	private float DefaultSpeed => this._ctx.Config.Editor.WorkcamMoveSpeed;

	private Vector3 Velocity;
	private Vector2 MouseDelta;
	private Vector3 InterpPos;

	private DateTime LastTime;
	
	public WorkCamera(
		ICameraManager manager,
		IEditorContext context
	) : base(manager) {
		this._ctx = context;
	}
	
	// Setup

	public void SetInitialPosition(Vector3 pos, Vector3 rot) {
		this.Position = pos;
		this.InterpPos = pos;
		this.Rotation = rot;
	}
	
	// Input handling

	public unsafe void UpdateControl(MouseDeviceData* mouseData, KeyboardDeviceData* keyData) {
		var leftHeld = false;
		var rightHeld = false;
		if (mouseData != null)
			this.UpdateMouse(mouseData, out leftHeld, out rightHeld);
		if (keyData != null)
			this.UpdateKeyboard(keyData, leftHeld, rightHeld);
	}

	private unsafe void UpdateMouse(MouseDeviceData* mouseData, out bool leftHeld, out bool rightHeld) {
		var delta = mouseData->GetDelta();
		leftHeld = mouseData->IsButtonHeld(MouseButton.Left);
		rightHeld = mouseData->IsButtonHeld(MouseButton.Right);
		if (rightHeld) this.MouseDelta += delta;
	}

	private unsafe void UpdateKeyboard(KeyboardDeviceData* keyData, bool leftHeld, bool rightHeld) {
		this.MoveSpeed = this.DefaultSpeed;
		if (keyData->IsKeyDown(VirtualKey.SHIFT)) // FreecamFast
			this.MoveSpeed *= this._ctx.Config.Editor.WorkcamFastMulti; // FreecamShiftMulti
		else if (keyData->IsKeyDown(VirtualKey.CONTROL)) // FreecamSlow
			this.MoveSpeed *= this._ctx.Config.Editor.WorkcamSlowMulti; // FreecamCtrlMulti

		var vFwb = 0;
		var bothHeld = leftHeld && rightHeld;
		if (IsKeyDown(keyData, VirtualKey.W) || bothHeld) vFwb -= 1; // Forward
		if (IsKeyDown(keyData, VirtualKey.S)) vFwb += 1; // Back

		var vLr = 0;
		if (IsKeyDown(keyData, VirtualKey.A)) vLr -= 1; // Left
		if (IsKeyDown(keyData, VirtualKey.D)) vLr += 1; // Right

		this.Velocity.X = vFwb * MathF.Sin(this.Rotation.X) * MathF.Cos(this.Rotation.Y) + (vLr * MathF.Cos(this.Rotation.X));
		this.Velocity.Y = vFwb * MathF.Sin(this.Rotation.Y);
		this.Velocity.Z = vFwb * MathF.Cos(this.Rotation.X) * MathF.Cos(this.Rotation.Y) + (-vLr * MathF.Sin(this.Rotation.X));

		if (IsKeyDown(keyData, VirtualKey.SPACE))
			this.Velocity.Y += this._ctx.Config.Editor.WorkcamVertMulti; // FreecamUpDownMulti
		if (IsKeyDown(keyData, VirtualKey.Q))
			this.Velocity.Y -= this._ctx.Config.Editor.WorkcamVertMulti;
	}

	private unsafe static bool IsKeyDown(KeyboardDeviceData* keyData, VirtualKey key)
		=> keyData->IsKeyDown(key, true);
	
	// View calculations

	public unsafe void Update() {
		var now = DateTime.Now;
		var delta = Math.Max((float)(now - this.LastTime).TotalMilliseconds, 1.0f);
		this.LastTime = now;
		
		var fov = Math.Abs(this.Camera->RenderEx->FoV);
		
		this.MouseDelta = this.MouseDelta * fov * this._ctx.Config.Editor.WorkcamSens * 0.0175f; // FreecamSensitivity * dampening
		this.Rotation.X -= this.MouseDelta.X;
		this.Rotation.Y = Math.Clamp(this.Rotation.Y + this.MouseDelta.Y, -ClampY, ClampY);
		this.MouseDelta = Vector2.Zero;
		
		this.Position += this.Velocity * this.MoveSpeed * fov;
		this.InterpPos = Vector3.Lerp(this.InterpPos, this.Position, MathF.Pow(0.5f, delta * 0.05f));
	}

	public Matrix4x4 CalculateViewMatrix() {
		var pos = this.InterpPos;
		var dir = this.CalculateLookDirection();
		var up = UpVector;

		var fLen = MathF.Sqrt(dir.X * dir.X + dir.Y * dir.Y + dir.Z * dir.Z);
		var f = dir / fLen;
		
		var s = new Vector3(
			up.Y * f.Z - up.Z * f.Y,
			up.Z * f.X - up.X * f.Z,
			up.X * f.Y - up.Y * f.X
		);
		var sLen = MathF.Sqrt(s.X * s.X + s.Y * s.Y + s.Z * s.Z);
		var sNorm = s / sLen;

		var u = new Vector3(
			f.Y * sNorm.Z - f.Z * sNorm.Y,
			f.Z * sNorm.X - f.X * sNorm.Z,
			f.X * sNorm.Y - f.Y * sNorm.X
		);

		var p = new Vector3(
			-pos.X * sNorm.X - pos.Y * sNorm.Y - pos.Z * sNorm.Z,
			-pos.X * u.X - pos.Y * u.Y - pos.Z * u.Z,
			-pos.X * f.X - pos.Y * f.Y - pos.Z * f.Z
		);

		return new Matrix4x4(
			sNorm.X, u.X, f.X, 0.0f,
			sNorm.Y, u.Y, f.Y, 0.0f,
			sNorm.Z, u.Z, f.Z, 0.0f,
			p.X, p.Y, p.Z, 1.0f
		);
	}

	private Vector3 CalculateLookDirection() => new(
		MathF.Sin(this.Rotation.X) * MathF.Cos(this.Rotation.Y),
		MathF.Sin(this.Rotation.Y),
		MathF.Cos(this.Rotation.X) * MathF.Cos(this.Rotation.Y)
	);
}
