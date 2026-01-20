using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Game;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Overlay;
using KamiToolKit.Extensions;

using Ktisis.Common.Extensions;
using Ktisis.Services.Plugin;

using Microsoft.Extensions.DependencyInjection;

namespace Ktisis.Interface.KTK;

public unsafe class PreviewNode : OverlayNode {
	public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;
	public override bool HideWithNativeUi => false;
	protected override void OnUpdate() { }

	private readonly ImageNode Image;
	private readonly RenderTargetManager* _renderTargetManager;
	private readonly AgentTryon* _agentTryon;
	private readonly IFramework _framework;
	private readonly IObjectTable _objectTable;
	private uint _counter;

	public PreviewNode(
		IFramework framework,
		IObjectTable objectTable) {
		this._framework = framework;
		this._objectTable =  objectTable;
		this._counter = 1;
		
		this._renderTargetManager = RenderTargetManager.Instance();
		this._agentTryon = AgentTryon.Instance();
		this.Image = new ImageNode() {
			Size = new Vector2(192.0f, 320.0f),
			ImageNodeFlags = (ImageNodeFlags)0x8C,
			WrapMode = WrapMode.Tile
		};
		var part = this.Image.AddPart(new Part());
		part->LoadTexture(this._renderTargetManager->CharaViewTextures[2]);
		this._agentTryon->CharaView.Initialize(null, 2, 0);
		this._renderTargetManager->CharaViewTextures[2].Value->IncRef();
		this._framework.Update += OnFramework;
		this.Image.AttachNode(this);
	}
	

	void OnFramework(IFramework framework) => this.Update();
	public void Update() {
		this._agentTryon->CharaView.Render(this._counter++);
		
	}
}
