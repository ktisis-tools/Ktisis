using System;
using System.Numerics;

using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using KamiToolKit;
using KamiToolKit.Overlay;

using Ktisis.Core.Attributes;
using Ktisis.Interface.KTK;

namespace Ktisis.Services.Game;

[Singleton]
public class OverlayService : IDisposable {
	private readonly IDalamudPluginInterface _dpi;
	private readonly IFramework _framework;

	private bool _init = false;
	private OverlayController _controller;
	private PreviewNode? _preview;

	public OverlayService(IDalamudPluginInterface dpi, IFramework framework) {
		this._dpi = dpi;
		this._framework = framework;
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

	public void ShowHint() {
		var r = new Random();
		this._controller.AddNode(new HintNode((uint)r.Next(73000, 73287), "This is a dummy hint bubble!! Wow", 450) {
			Position = new Vector2(400.0f, 400.0f),
			Size = new Vector2(640.0f, 256.0f),
			CollisionNode = {
				Position = new Vector2(-99.0f, -155.0f),
				Size = new Vector2(749.0f, 256.0f)
			}
		});
	}

	public void ToggleCharaViewTexture() {
		if (this._preview != null) { // currently isnt cleared when exiting gpose
			this._controller.RemoveNode(this._preview);
			this._preview.Dispose();
			this._preview = null;
			return;
		}
		this._preview = new PreviewNode() {
			Position = new Vector2(500.0f, 500.0f)
		};
		this._controller.AddNode(this._preview);
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
