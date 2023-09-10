using System.Numerics;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

using Ktisis.Core.Impl;

using SceneCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera;
using RenderCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Camera;

namespace Ktisis.Core.Services; 

[KtisisService]
public class CameraService {
	// Constructor

	public CameraService() {}

	// Camera access

	public unsafe Camera* GetGameCamera() {
		var manager = CameraManager.Instance;
		return manager != null ? manager->GetActiveCamera() : null;
	}

	public unsafe SceneCamera* GetSceneCamera() {
		var cam = GetGameCamera();
		return cam != null ? &cam->CameraBase.SceneCamera : null;
	}

	public unsafe RenderCamera* GetRenderCamera() {
		var cam = GetSceneCamera();
		return cam != null ? cam->RenderCamera : null;
	}

	// Camera matrix access

	public unsafe Matrix4x4? GetProjectionMatrix() {
		var camera = GetRenderCamera();
		if (camera == null)
			return null;
		return camera->ProjectionMatrix;
	}

	public unsafe Matrix4x4? GetViewMatrix() {
		var camera = GetSceneCamera();
		if (camera == null)
			return null;
		return camera->ViewMatrix with { M44 = 1f };
	}
}
