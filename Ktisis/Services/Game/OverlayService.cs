using System;
using System.Numerics;

using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using KamiToolKit;
using KamiToolKit.Overlay;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.KTK;
using Ktisis.Scene.Entities.Game;

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
		context.Plugin.Gui.FileDialogs.OnSelectionChanged += this.HandleFileDialogEvent;
		
		if (context.Config.Editor.ShowHints && !this._showedHint)
			this.ShowHint(context);
		ToggleCharaViewTexture(context);
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

	public void SetCharaViewData(ActorEntity actor, IEditorContext ctx) => this._preview?.UpdateActorData(actor);
	public void ToggleCharaViewTexture(IEditorContext context) {
		this._preview = new PreviewNode(context, this._framework, this._objectTable) {
			Position = new Vector2(500.0f, 500.0f)
		};
		this._controller?.AddNode(this._preview);
		if (context.Selection.Count == 1) {
			SetCharaViewData((ActorEntity)context.Selection.GetFirstSelected(), context);
		}

	}

	public void HandleFileDialogEvent(object? sender, string path) {

		this._preview.PoseActor(path);
		
		var extension = path.Substring(path.LastIndexOf('.') + 1).ToLower();

		if (extension == "pose" || extension == "cmp") {
			this._preview.PoseActor(path);
		}/*else if (extension == "chara") {
			this._preview.LoadChara(path);
		}else if (extension == "mcdf") {
			this._preview.LoadMcdf(path);
		}*/
	}

	public bool RemoveNode(OverlayNode node) {
		this._controller?.RemoveNode(node);
		return true;
	}

	public void Disable() {
		if (!this._init) return;

		if (this._preview != null) {
			this._controller?.RemoveNode(this._preview);
			this._preview.Dispose();
			this._preview = null;
		}
		this._controller?.Dispose();
		KamiToolKitLibrary.Dispose();
		this._init = false;
	}

	public void Dispose() {
		this.Disable();
	}
}
