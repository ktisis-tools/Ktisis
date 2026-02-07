using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;

using Dalamud.Bindings.ImGui;
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
	public override bool IsVisible { get; set; } = false;
	protected override void OnUpdate() { }

	private readonly ImageNode Image;
	private readonly NineGridNode Border;
	private uint _counter;
	
	private readonly RenderTargetManager* _renderTargetManager;
	private readonly AgentTryon* _agentTryon;
	
	private readonly IFramework _framework;
	private readonly IObjectTable _objectTable;

	private ImGuiWindowPtr _fileWindow;

	public PreviewNode(
		IEditorContext context,
		IFramework framework,
		IObjectTable objectTable
	) {
		this._framework = framework;
		this._objectTable = objectTable;
		this._counter = 1;
		this._fileWindow = null;


		var needsInit = false;
		this._renderTargetManager = RenderTargetManager.Instance();
		this._agentTryon = AgentTryon.Instance();  //idk why this was below the eval before?
		if(this._agentTryon == null)
			needsInit = true;


		if (needsInit) {
			this._framework.RunOnFrameworkThread(() => {
				this._agentTryon->Update(1);
				this._agentTryon->Hide();
			});
		}
		this.Image = new ImageNode() {
			Size = new Vector2(192.0f, 320.0f),
			Position = new Vector2(4, 3),
			ImageNodeFlags = (ImageNodeFlags)0x8C,
			WrapMode = WrapMode.Tile
		};
		this.Border = new NineGridNode() {
			Size = new Vector2(200.0f, 328.0f),
			TopOffset = 14.0f,
			LeftOffset = 14.0f,
			RightOffset = 14.0f,
			BottomOffset = 14.0f
		};
		this.Border.AddPart(new Part {
			TexturePath = "ui/uld/PreviewA_hr1.tex",
			Size = new Vector2(36.0f, 36.0f),
			TextureCoordinates = new Vector2( 0, 0f),
			Id = 0
		});
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
		this.Border.AttachNode(this);
	}

	private void OnFramework(IFramework framework) {
		this._agentTryon->CharaView.Render(this._counter++);

		if (this._fileWindow.IsNull) {
			this.IsVisible = false;
			this._fileWindow = ImGuiP.FindWindowByName("Open Pose File###OpenFileDialog");
		} else if (this._fileWindow.Active == false)
		{
			this.IsVisible = false;
		}
		else {
			this.IsVisible = true;
			this.Position = new Vector2(this._fileWindow.Pos.X + this._fileWindow.Size.X, this._fileWindow.Pos.Y);
		}

	}

	private string TryFetchSelectedFile() {
		string filePath = String.Empty;
		return filePath;
	}
}
