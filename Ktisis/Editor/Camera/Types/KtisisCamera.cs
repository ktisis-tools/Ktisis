using System;

using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;

using Ktisis.Interop;

namespace Ktisis.Editor.Camera.Types;

public class KtisisCamera : EditorCamera, IDisposable {
	private Alloc<GameCamera>? Alloc = new();

	public override nint Address => this.Alloc?.Address ?? nint.Zero;

	public KtisisCamera(
		ICameraManager manager
	) : base(manager) {
		
	}

	// Disposal
	
	public void Dispose() {
		this.Alloc?.Dispose();
		this.Alloc = null;
		GC.SuppressFinalize(this);
	}
}
