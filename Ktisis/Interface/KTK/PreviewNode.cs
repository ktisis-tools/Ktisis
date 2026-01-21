using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Game;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Overlay;
using KamiToolKit.Extensions;

using Ktisis.Common.Extensions;
using Ktisis.Editor.Context.Types;
using Ktisis.Scene.Entities.Game;
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
	private readonly IEditorContext _ctx;

	public PreviewNode(
		IEditorContext context,
		IFramework framework,
		IObjectTable objectTable
	) {
		this._ctx = context;
		this._framework = framework;
		this._objectTable = objectTable;
		this._counter = 1;

		
		var needsInit = false;
		this._renderTargetManager = RenderTargetManager.Instance();
		if(this._agentTryon == null)
			needsInit = true;

		this._agentTryon = AgentTryon.Instance();
		if (needsInit) {
			this._framework.RunOnFrameworkThread(() => {
				this._agentTryon->Update(1);
				this._agentTryon->Hide();
			});
		}
		this.Image = new ImageNode() {
			Size = new Vector2(192.0f, 320.0f),
			ImageNodeFlags = (ImageNodeFlags)0x8C,
			WrapMode = WrapMode.Tile
		};
		var part = this.Image.AddPart(new Part());
		part->LoadTexture(this._renderTargetManager->CharaViewTextures[2]);
		this._renderTargetManager->CharaViewTextures[2].Value->IncRef();

		this._framework.RunOnFrameworkThread(() => {
			this._agentTryon->CharaView.Initialize(&this._agentTryon->AgentInterface, 2, 0);
			var modelData = this._agentTryon->CharaView.ModelData;
			modelData.CopyFromCharacter((Character*)this._objectTable.LocalPlayer?.Address);
			this._agentTryon->CharaView.SetModelData(&modelData);
		});

		// if (this._ctx.Selection.GetFirstSelected() is ActorEntity actor) {
		// 	var modelData = this._agentTryon->CharaView.ModelData;
		// 	modelData.CopyFromCharacter(actor.Character);
		// 	this._agentTryon->CharaView.SetModelData(&modelData);
		// }
		this._framework.Update += this.OnFramework;
		this.Image.AttachNode(this);
	}

	private void OnFramework(IFramework framework) {
		this._agentTryon->CharaView.Render(this._counter++);
	}
}
