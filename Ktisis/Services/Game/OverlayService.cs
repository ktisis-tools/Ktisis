using System;
using System.Numerics;

using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using KamiToolKit;
using KamiToolKit.Overlay;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.KTK;

namespace Ktisis.Services.Game;

[Singleton]
public class OverlayService : IDisposable {
	private readonly IDalamudPluginInterface _dpi;
	private readonly IFramework _framework;
	private readonly IObjectTable _objectTable;

	private bool _init = false;
	private bool _showedHint = false;
	private OverlayController? _controller;
	private PreviewNode? _preview;

	public OverlayService(IDalamudPluginInterface dpi, IFramework framework, IObjectTable objectTable) {
		this._dpi = dpi;
		this._framework = framework;
		this._objectTable =  objectTable;
	}

	public void Initialize(IEditorContext context) {
		if (this._init) return;
		KamiToolKitLibrary.Initialize(this._dpi);
		this._controller = new OverlayController();
		this._init = true;

		if (context.Config.Editor.ShowHints && !this._showedHint)
			this.ShowHint(context);
	}

	public bool AddNode(OverlayNode node) {
		this._controller?.AddNode(node);
		return true;
	}

	public void ShowHint(IEditorContext context) {
		var r = new Random();
		var icon = r.Next(73001, 73288);
		var key = context.Locale.RandomHintKey();
		var hint = context.Locale.Translate($"hints.{key}");

		this._controller?.AddNode(new HintNode((uint)icon, hint, key, 300) {
			Position = new Vector2(87.0f, 138.0f),
			Size = new Vector2(640.0f, 80.0f),
			Scale = new Vector2(1.0f, 1.0f),
			CollisionNode = {
				Position = new Vector2(-99.0f, -155.0f),
				Size = new Vector2(749.0f, 256.0f)
			}
		});
		this._showedHint = true;
	}

	public void ToggleCharaViewTexture() {
		if (this._preview != null) { // currently isnt cleared when exiting gpose
			this._controller?.RemoveNode(this._preview);
			this._preview.Dispose();
			this._preview = null;
			return;
		}
		this._preview = new PreviewNode(this._framework, this._objectTable) {
			Position = new Vector2(500.0f, 500.0f)
		};
		this._controller?.AddNode(this._preview);
	}

	public bool RemoveNode(OverlayNode node) {
		this._controller?.RemoveNode(node);
		return true;
	}

	public void Disable() {
		if (!this._init) return;
		this._controller?.Dispose();
		KamiToolKitLibrary.Dispose();
		this._init = false;
	}

	public void Dispose() {
		this.Disable();
	}
}
