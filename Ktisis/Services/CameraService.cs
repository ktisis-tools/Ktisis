using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using SceneCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera;
using RenderCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Camera;

namespace Ktisis.Services;

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
}
