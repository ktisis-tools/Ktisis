using System.Numerics;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Overlay;
using KamiToolKit.Extensions;

namespace Ktisis.Interface.KTK;

public unsafe class PreviewNode : OverlayNode {
	public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;
	public override bool HideWithNativeUi => false;
	protected override void OnUpdate() { }

	private readonly ImageNode Image;
	private readonly RenderTargetManager* _renderTargetManager;
	private readonly AgentTryon* _agentTryon;

	public PreviewNode() {
		this._renderTargetManager = RenderTargetManager.Instance();
		this._agentTryon = AgentTryon.Instance();
		this.Image = new ImageNode() {
			Size = new Vector2(192.0f, 320.0f),
			ImageNodeFlags = (ImageNodeFlags)0x8C,
			WrapMode = WrapMode.Tile
		};
		var part = this.Image.AddPart(new Part());
		part->LoadTexture(this._renderTargetManager->CharaViewTextures[2]);
		this._renderTargetManager->CharaViewTextures[2].Value->IncRef();
		this._agentTryon->CharaView.VirtualTable->Initialize(&this._agentTryon->CharaView, &this._agentTryon->AgentInterface, 0, 0);
		this.Image.AttachNode(this);
	}
}
