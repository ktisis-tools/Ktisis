using System.Numerics;

using Dalamud.Interface.Utility;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using SceneCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera;
using RenderCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Camera;

namespace Ktisis.Services.Game; 

public static class CameraService {
	// Camera access
    
	public unsafe static Camera* GetGameCamera() {
		var manager = CameraManager.Instance();
		return manager != null ? manager->GetActiveCamera() : null;
	}

	public unsafe static SceneCamera* GetSceneCamera() {
		var cam = GetGameCamera();
		return cam != null ? &cam->CameraBase.SceneCamera : null;
	}

	public unsafe static RenderCamera* GetRenderCamera() {
		var cam = GetSceneCamera();
		return cam != null ? cam->RenderCamera : null;
	}

	public unsafe static Matrix4x4? GetProjectionMatrix() {
		var camera = GetRenderCamera();
		if (camera == null)
			return null;
		
		var p = camera->ProjectionMatrix;
		
		var far = camera->FarPlane;
		var near = camera->NearPlane;
		var clip = far / (far - near);
		p.M33 = -((far + near) / (far - near));
		p.M43 = -(clip * near);
			
		return p;
	}

	public unsafe static Matrix4x4? GetViewMatrix() {
		var camera = GetSceneCamera();
		if (camera == null) return null;
		return camera->ViewMatrix with { M44 = 1f };
	}
	
	// World to screen conversion

	public unsafe static bool WorldToScreen(SceneCamera* camera, Vector3 worldPos, out Vector2 screenPos) {
		var viewMatrix = camera->ViewMatrix;
		if (camera->RenderCamera->IsOrtho)
			viewMatrix = viewMatrix with { M44 = 1f };
		var matrix = viewMatrix * camera->RenderCamera->ProjectionMatrix;
		var result = WorldToScreenDepth(matrix, worldPos, out var pos2d);
		screenPos = new Vector2(pos2d.X, pos2d.Y);
		return result;
	}
	
	private static bool WorldToScreenDepth(Matrix4x4 m, Vector3 v, out Vector3 screenPos) {
		var x = (m.M11 * v.X) + (m.M21 * v.Y) + (m.M31 * v.Z) + m.M41;
		var y = (m.M12 * v.X) + (m.M22 * v.Y) + (m.M32 * v.Z) + m.M42;
		var w = (m.M14 * v.X) + (m.M24 * v.Y) + (m.M34 * v.Z) + m.M44;

		var view = ImGuiHelpers.MainViewport;
		
		var camX = (view.Size.X / 2f);
		var camY = (view.Size.Y / 2f);
		screenPos = new Vector3(
			camX + (camX * x / w) + view.Pos.X,
			camY - (camY * y / w) + view.Pos.Y,
			w
		);

		return w > 0.001f;
	}
}
