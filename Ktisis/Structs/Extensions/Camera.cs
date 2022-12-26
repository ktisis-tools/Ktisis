using System;
using System.Numerics;

using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;

namespace Ktisis.Structs.Extensions {
	public static class Camera {
		public unsafe static Matrix4x4 GetProjectionMatrix(this GameCamera camera) {
			var ptr = (byte*)&camera;
			return *(Matrix4x4*)((IntPtr)camera.CameraBase.SceneCamera.RenderCamera + 80);
		}
		public unsafe static Matrix4x4 GetViewMatrix(this GameCamera camera) {
			var ptr = (byte*)&camera;
			var view = *(Matrix4x4*)(ptr + 0xB0);
			view.M44 = 1;
			return view;
		}
		public unsafe static Vector3 GetCameraPos(this GameCamera camera) {
			var ptr = (byte*)&camera;
			return *(Vector3*)((IntPtr)camera.CameraBase.SceneCamera.RenderCamera + 144);
		}
	}
}