using System;
using System.Numerics;

using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;

namespace Ktisis.Structs.Extensions {
	public static class Camera {
		public unsafe static Matrix4x4 GetProjectionMatrix(this GameCamera camera) {
			var cam = camera.CameraBase.SceneCamera.RenderCamera;
			var proj = cam->ProjectionMatrix;
			var clip = cam->FarPlane / (cam->FarPlane - cam->NearPlane);
			//proj.M43 = -(clip * cam->NearPlane);
			proj.M33 = -clip;
			return proj;
		}
		public unsafe static Matrix4x4 GetViewMatrix(this GameCamera camera) {
			var view = camera.CameraBase.SceneCamera.ViewMatrix;
			view.M44 = 1;
			return view;
		}
		public unsafe static Vector3 GetCameraPos(this GameCamera camera) {
			var ptr = (byte*)&camera;
			return *(Vector3*)((IntPtr)camera.CameraBase.SceneCamera.RenderCamera + 144);
		}

		public static float DistanceFrom(this GameCamera camera, Vector3 vec)
			=> Vector3.Distance(vec, camera.CameraBase.SceneCamera.Object.Position);
	}
}
