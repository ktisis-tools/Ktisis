using System;

using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;

using Ktisis.Interop;
using Ktisis.Interop.Hooks;
using Ktisis.Structs.FFXIV;

namespace Ktisis.Camera {
	public class KtisisCamera : IDisposable {
		private readonly GameAlloc<GameCamera> Alloc;
		
		public string Name = "New Camera";
		public bool IsFreecam = false;

		public nint Address => Alloc.Address;
		public unsafe GameCamera* GameCamera => Alloc.Data;

		// Constructors
		
		internal KtisisCamera(GameAlloc<GameCamera> alloc) => Alloc = alloc;

		public unsafe static KtisisCamera Spawn(GameCamera* clone = null) {
			var alloc = new GameAlloc<GameCamera>();
			CameraHooks.GameCamera_Ctor(alloc.Data);
			if (clone != null)
				*alloc.Data = *clone;
			return new KtisisCamera(alloc);
		}
		
		// Helpers

		public unsafe GPoseCamera* AsGPoseCamera()
			=> (GPoseCamera*)Address;
		
		// IDisposable

		public void Dispose() => Alloc.Dispose();
	}
}