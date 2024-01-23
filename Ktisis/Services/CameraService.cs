using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using SceneCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera;
using RenderCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Camera;

using Ktisis.Core.Attributes;
using Ktisis.Structs.Camera;

namespace Ktisis.Services; 

[Singleton]
public class CameraService {
	private readonly IGameGui _gui;
	
	public CameraService(
		IGameGui gui,
		IGameInteropProvider interop
	) {
		this._gui = gui;
		interop.InitializeFromAttributes(this);
	}
	
	// Camera matrices
    
	public unsafe Camera* GetGameCamera() {
		var manager = CameraManager.Instance();
		return manager != null ? manager->GetActiveCamera() : null;
	}

	public unsafe SceneCamera* GetSceneCamera() {
		var cam = this.GetGameCamera();
		return cam != null ? &cam->CameraBase.SceneCamera : null;
	}

	public unsafe RenderCamera* GetRenderCamera() {
		var cam = this.GetSceneCamera();
		return cam != null ? cam->RenderCamera : null;
	}

	public unsafe Matrix4x4? GetProjectionMatrix() {
		var camera = this.GetRenderCamera();
		if (camera == null)
			return null;
		return camera->ProjectionMatrix;
	}

	public unsafe Matrix4x4? GetViewMatrix() {
		var camera = this.GetSceneCamera();
		if (camera == null)
			return null;
		return camera->ViewMatrix with { M44 = 1f };
	}
	
	// World matrix

	public unsafe bool WorldToScreen(Vector3 worldPos, out Vector2 screenPos) {
		var matrix = this.GetMatrix != null ? this.GetMatrix() : null;
		if (matrix == null)
			return this._gui.WorldToScreen(worldPos, out screenPos);
		var result = matrix->WorldToScreenDepth(worldPos, out var pos2d);
		screenPos = new Vector2(pos2d.X, pos2d.Y);
		return result;
	}

	[Signature("E8 ?? ?? ?? ?? 48 8D 4C 24 ?? 48 89 4c 24 ?? 4C 8D 4D ?? 4C 8D 44 24 ??")]
	private GetMatrixDelegate? GetMatrix = null;
	
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private unsafe delegate WorldMatrix* GetMatrixDelegate();
}
