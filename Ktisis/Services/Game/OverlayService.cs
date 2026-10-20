using System;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using KamiToolKit;
using KamiToolKit.Overlay;
using KamiToolKit.Overlay.UiOverlay;

using Ktisis.Core.Attributes;
using Ktisis.Data.Config.Sections;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.KTK;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Services.Game;

[Singleton]
public class OverlayService : IDisposable {
	private readonly IDalamudPluginInterface _dpi;
	private readonly IFramework _framework;
	private readonly IObjectTable _objectTable;
	private const float HintOffsetX = 87.0f;
	private const float HintOffsetY = 138.0f;
	private const float HintSizeX = 640.0f;
	private const float HintSizeY = 80.0f;

	private bool _init = false;
	private bool _showedHint = false;
	private OverlayController? _controller;
	private PreviewNode? _preview;

	public OverlayService(IDalamudPluginInterface dpi, IFramework framework, IObjectTable objectTable) {
		this._dpi = dpi;
		this._framework = framework;
		this._objectTable =  objectTable;
	}

	public unsafe void Initialize(IEditorContext context) {
		if (this._init) return;
		KamiToolKitLibrary.Initialize(this._dpi);
		this._controller = new OverlayController();
		this._init = true;
		context.Plugin.Gui.FileDialogs.OnSelectionChanged += this.HandleFileDialogEvent;

		if (context.Config.Editor.ShowHints && !this._showedHint)
			this.ShowHint(context.Config.Editor.HintLocation);
	}

	public bool AddNode(OverlayNode node) {
		this._controller?.AddNode(node);
		return true;
	}

	private void ShowHint(HintLoc location) {
		var r = new Random();
		var icon = r.Next(73001, 73291);
		var key = Ktisis.Locale.RandomHintKey();
		var hint = Ktisis.Locale.Translate($"hints.{key}");

		this._controller?.AddNode(new HintNode((uint)icon, hint, key, 300) {
			Position = GetPositionForLoc(location),
			Size = new Vector2(HintSizeX, HintSizeY),
			Scale = new Vector2(1.0f, 1.0f),
			CollisionNode = {
				Position = new Vector2(-99.0f, -155.0f),
				Size = new Vector2(749.0f, 256.0f)
			}
		});
		this._showedHint = true;
	}

	public unsafe void ToggleCharaViewTexture(IEditorContext context, ActorEntity actor) {
		this.DisablePreview();
		if (actor.GetHuman() != null && actor.Appearance.ModelId is 0 or null) {
			this._preview = new PreviewNode(context, this._framework, this._objectTable, actor) {
				Position = new Vector2(500.0f, 500.0f)
			};
			this._controller?.AddNode(this._preview);
		}
	}

	private void HandleFileDialogEvent(object? sender, string path) {
		var extension = path[(path.LastIndexOf('.') + 1)..].ToLower();

		if (extension is "pose" or "cmp") {
			this._preview?.PoseActor(path);
		}
	}

	public bool RemoveNode(OverlayNode node) {
		this._controller?.RemoveNode(node);
		return true;
	}

	public void Disable() {
		if (!this._init) return;

		this.DisablePreview();
		this._controller?.Dispose();
		KamiToolKitLibrary.Dispose();
		this._init = false;
	}

	private void DisablePreview() {
		if (this._preview == null) return;
		this._controller?.RemoveNode(this._preview);
		this._preview.Cleanup();
		this._preview = null;
	}

	private static Vector2 GetPositionForLoc(HintLoc loc) => loc switch {
		HintLoc.TopLeft => new Vector2(HintOffsetX, HintOffsetY),
		HintLoc.TopRight => new Vector2(ImGui.GetMainViewport().Size.X - HintSizeX, HintOffsetY),
		HintLoc.BottomLeft => new Vector2(HintOffsetX, ImGui.GetMainViewport().Size.Y - HintSizeY),
		HintLoc.BottomRight => new Vector2(ImGui.GetMainViewport().Size.X - HintSizeX, ImGui.GetMainViewport().Size.Y - HintSizeY),
		_ => throw new ArgumentOutOfRangeException()
	};

	public void Dispose() {
		this.Disable();
	}
}
