using System;

using Dalamud.Plugin;

using KamiToolKit;
using KamiToolKit.Overlay;

using Ktisis.Core.Attributes;
using Ktisis.Interface.KTK;

namespace Ktisis.Services.Game;

[Singleton]
public class OverlayService : IDisposable {
	private OverlayController _controller;
	private readonly IDalamudPluginInterface _dpi;
	private bool _init = false;

	public OverlayService(IDalamudPluginInterface dpi) {
		this._dpi = dpi;
		this.Initialize();
	}

	public void Initialize() {
		if (this._init) return;
		KamiToolKitLibrary.Initialize(this._dpi);
		this._controller = new OverlayController();
		this._init = true;
	}

	public bool AddNode(OverlayNode node) {
		this._controller.AddNode(node);
		return true;
	}

	public bool RemoveNode(OverlayNode node) {
		this._controller.RemoveNode(node);
		return true;
	}

	public void Dispose() {
		this._controller.Dispose();
		KamiToolKitLibrary.Dispose();
		this._init = false;
	}
}
