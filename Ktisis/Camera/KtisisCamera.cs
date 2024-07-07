using System;
using System.Linq;
using System.Numerics;

using Dalamud.Game.ClientState.Objects.Types;

using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;

using Ktisis.Interop;
using Ktisis.Interop.Hooks;
using Ktisis.Structs.FFXIV;

namespace Ktisis.Camera {
	public class KtisisCamera : IDisposable {
		// Camera stuff :)
		
		public string Name = "New Camera";
		
		public bool IsNative;
		public CameraEdit CameraEdit = new();
		public WorkCamera? WorkCamera;
		
		internal nint ClonedFrom;
		
		// Memory access
		
		private readonly GameAlloc<GameCamera>? Alloc;
		
		private nint _address;
		public nint Address => IsNative ? _address : (Alloc?.Address ?? 0);
		
		public unsafe GameCamera* GameCamera => (GameCamera*)Address;

		// Constructors

		internal KtisisCamera(nint addr) {
			IsNative = true;
			_address = addr;
		}
		
		internal KtisisCamera(GameAlloc<GameCamera> alloc) => Alloc = alloc;
		
		// Static constructors

		// This assumes that native cameras will never be freed from memory while ingame.
		public unsafe static KtisisCamera Native(GameCamera* address)
			=> new((nint)address);

		public unsafe static KtisisCamera Spawn(GameCamera* clone = null) {
			var alloc = new GameAlloc<GameCamera>();
			CameraHooks.GameCamera_Ctor(alloc.Data);
			var camera = new KtisisCamera(alloc);
			if (clone != null) {
				*alloc.Data = *clone;
				camera.ClonedFrom = (nint)clone;
			}
			return camera;
		}
		
		// Helpers

		public bool IsValid() => Address != 0;

		public unsafe GPoseCamera* AsGPoseCamera() => (GPoseCamera*)Address;

		public unsafe Vector3 Position => IsValid() ? AsGPoseCamera()->Position : default;
		public unsafe Vector3 Rotation => IsValid() ? AsGPoseCamera()->CalcRotation() : default;
		
		// Camera edits

		public void SetOrbit(ushort? id) => CameraEdit.Orbit = id;
		public void SetPositionLock(Vector3? pos) => CameraEdit.Position = pos;
		public void SetOffset(Vector3? off) => CameraEdit.Offset = off;
		
		internal IGameObject? GetOrbitTarget() {
			if (!Ktisis.IsInGPose) return null;
			return CameraEdit.Orbit != null ? Services.ObjectTable.FirstOrDefault(
				actor => actor.ObjectIndex == CameraEdit.Orbit
			) : null;
		}
		
		// IDisposable

		public void Dispose() {
			Alloc?.Dispose();
			WorkCamera = null;
			_address = 0;
		}
	}
	
	public class CameraEdit {
		public ushort? Orbit;
		public Vector3? Position;
		public Vector3? Offset;
		public bool NoClip = false;

		public CameraEdit Clone() {
			var result = new CameraEdit();
			foreach (var field in GetType().GetFields())
				field.SetValue(result, field.GetValue(this));
			return result;
		}
	}
}