using System;

using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;

using Ktisis.Interop;
using Ktisis.Interop.Hooks;

namespace Ktisis.Camera {
	public class KtisisCamera : IDisposable {
		private readonly GameAlloc<GameCamera> Alloc;

		public nint Address => Alloc.Address;
		public unsafe GameCamera* GameCamera => Alloc.Data;

		public string Name = "New Camera";

		// Constructors
		
		internal KtisisCamera(GameAlloc<GameCamera> alloc) => Alloc = alloc;

		public unsafe static KtisisCamera Spawn(GameCamera* clone = null) {
			var alloc = new GameAlloc<GameCamera>();
			CameraHooks.GameCamera_Ctor(alloc.Data);
			if (clone != null)
				*alloc.Data = *clone;
			return new KtisisCamera(alloc);
		}
		
		// IDisposable

		public void Dispose() => Alloc.Dispose();
	}
}