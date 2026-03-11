using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Overlay;
using KamiToolKit.Extensions;

using Ktisis.Data.Files;
using Ktisis.Data.Json;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Data;
using Ktisis.Editor.Posing.Ik;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Factory.Builders;

using Newtonsoft.Json;


namespace Ktisis.Interface.KTK;

public unsafe class PreviewNode : OverlayNode {
	public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;
	public override bool HideWithNativeUi => false;
	public override bool IsVisible { get; set; } = false;
	protected override void OnUpdate() { }

	private readonly ImageNode Image;
	private readonly NineGridNode Border;
	private readonly NodeBase Buttons;
	private List<ButtonBase> buttonList = new List<ButtonBase>();
	
	private uint _counter;
	private ActorEntity _actor;
	private float _pitch = 0;
	private float _yaw = 0;

	private readonly RenderTargetManager* _renderTargetManager;
	private readonly AgentInspect* _agentInspect;
	private ImGuiWindowPtr _fileWindow;

	private readonly IFramework _framework;
	private readonly IObjectTable _objectTable;
	private readonly IEditorContext _ctx;
	private readonly JsonFileSerializer _serializer;
	

	public PreviewNode(
		IEditorContext context,
		IFramework framework,
		IObjectTable objectTable,
		ActorEntity target
	) {
		if (target.GetHuman() == null)
			return;
		
		this._framework = framework;
		this._objectTable = objectTable;
		this._counter = 1;
		this._fileWindow = null;
		this._ctx = context;
		this._serializer = new JsonFileSerializer();
		
		
		this._renderTargetManager = RenderTargetManager.Instance();
		this._agentInspect = AgentInspect.Instance(); //idk why this was below the eval before?




		this.Image = new ImageNode() {
			Size = new Vector2(192.0f, 320.0f),
			Position = new Vector2(4, 3),
			ImageNodeFlags = (ImageNodeFlags)0x8C,
			WrapMode = WrapMode.Tile,
		};
		this.Border = new NineGridNode() {
			Size = new Vector2(200.0f, 328.0f),
			TopOffset = 14.0f,
			LeftOffset = 14.0f,
			RightOffset = 14.0f,
			BottomOffset = 14.0f,
		};
		this.Border.AddPart(new Part {
			TexturePath = "ui/uld/PreviewA_hr1.tex",
			Size = new Vector2(36.0f, 36.0f),
			TextureCoordinates = new Vector2(0, 0f),
			Id = 0
		});
		
		var part = this.Image.AddPart(new Part());
		part->LoadTexture(this._renderTargetManager->CharaViewTextures[1]);
		this._renderTargetManager->CharaViewTextures[1].Value->IncRef();

		this._framework.RunOnFrameworkThread(() => {
			this._agentInspect->CharaView.Initialize(&this._agentInspect->AgentInterface, 1, 0);
			this._agentInspect->CharaView.ModelData.CopyFromCharacter((Character*)target.Actor.Address);
		});

		Buttons = this.SetupButtons();

		_actor = new ActorEntity(this._ctx.Scene, new PoseBuilder(this._ctx.Scene), this._objectTable[441]);
		this._actor.Setup();
		this._framework.Update += this.OnFramework;


		this.Image.AttachNode(this);
		this.Border.AttachNode(this);
		this.Buttons.AttachNode(this);
	}

	/// <summary>
	/// Framework update for our preview window, required to work 
	/// </summary>
	private void OnFramework(IFramework framework) {
		this._agentInspect->CharaView.Render(this._counter++);


		this._fileWindow = ImGuiP.FindWindowByName("###OpenFileDialog");
		if (!this._ctx.Plugin.Gui.FileDialogs.IsDialogOpen()) {
			
			this.Cleanup();
			return; //lets try to not overflow the games renderer
		}

		this.IsVisible = true;
		this.Position = new Vector2(this._fileWindow.Pos.X + this._fileWindow.Size.X, this._fileWindow.Pos.Y);


	}

	public void MoveCamera(float pitch, float yaw) {
		this._pitch += pitch;
		this._yaw += yaw;
		this._agentInspect->CharaView.SetCameraYawAndPitch(yaw, pitch);
		//this._agentInspect->CharaView.Camera->Rotation
	}

	/// <summary>
	/// Resets the camera position
	/// </summary>
	public void ResetCamera() {
		this._agentInspect->CharaView.SetCameraYawAndPitch(this._yaw * -1 , this._pitch * -1);
		this._pitch = 0;
		this._yaw = 0;
	}

	/// <summary>
	/// Poses the actor in the preview window
	/// </summary>
	/// <param name="path">Path of the pose file</param>
	public void PoseActor(string path) {
		var content = File.ReadAllText(path);
		if (Path.GetExtension(path).Equals(".cmp")) content = LegacyPoseHelpers.ConvertLegacyPose(content);
		var file = this._serializer.Deserialize<PoseFile>(content);
		this._ctx.Posing.ApplyPoseFile(_actor.Pose, file, transforms: PoseTransforms.Rotation);
	}


	public void Cleanup() {
		this._framework.Update -= this.OnFramework;
		this._agentInspect->CharaView.Release();
		this.Dispose();
	}
	
	public NodeBase SetupButtons() {

		NodeBase node = new ResNode() {
			Size =  new Vector2(168.0f, 32.0f),
			Position = new Vector2(8f, 286f),
			Priority = 1
		};
		
		/*	ButtonBase buttonDown = new CircleButtonNode() {
			Icon = ButtonIcon.ArrowDown,
			Position = new Vector2(0f , 0f),
			Size = new Vector2(32.0f, 32.0f),
		};
		buttonDown.OnClick = () => {
			this.MoveCamera(-25, 0);
		};
		this.buttonList.Add(buttonDown);
		
		ButtonBase buttonUp = new CircleButtonNode() {
			Icon = ButtonIcon.UpArrow,
			Position = new Vector2(32f, 0f),
			Size = new Vector2(32.0f, 32.0f),
		};
		buttonUp.OnClick = () => {
			this.MoveCamera(25, 0);
		};
		this.buttonList.Add(buttonUp);*/
		
		ButtonBase buttonLeft = new CircleButtonNode() {
			Icon = ButtonIcon.RightArrow,
			Position = new Vector2(64f, 0f),
			Size = new Vector2(32.0f, 32.0f),
			Scale = new Vector2(-1f, 1f),
		};
		buttonLeft.OnClick = () => {
			this.MoveCamera(0, -25f);
		};
		this.buttonList.Add(buttonLeft);
		
		ButtonBase buttonRight = new CircleButtonNode() {
			Icon = ButtonIcon.RightArrow,
			Position = new Vector2(64f, 0f),
			Size = new Vector2(32.0f, 32.0f),
		};
		buttonRight.OnClick = () => {
			this.MoveCamera(0, 25f);
		};
		this.buttonList.Add(buttonRight);

		ButtonBase buttonReset = new CircleButtonNode() {
			Icon = ButtonIcon.Undo,
			Position = new Vector2(148f, 0f),
			Size = new Vector2(32.0f, 32.0f),
		};
		buttonReset.OnClick = () => {
			this.ResetCamera();
		};
		this.buttonList.Add(buttonReset);
		
		foreach (var b in this.buttonList) {
			b.AttachNode(node);
		}
		return node;
	}
}
