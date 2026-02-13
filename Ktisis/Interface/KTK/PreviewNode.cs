using System;
using System.IO;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

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
	private uint _counter;
	private ActorEntity _actor;
	private bool _doUpdate;
	
	private readonly RenderTargetManager* _renderTargetManager;
	private readonly AgentTryon* _agentTryon;
	private ImGuiWindowPtr _fileWindow;
	
	private readonly IFramework _framework;
	private readonly IObjectTable _objectTable;
	private readonly IEditorContext _ctx;
	private readonly JsonFileSerializer _serializer;

	

	

	public PreviewNode(
		IEditorContext context,
		IFramework framework,
		IObjectTable objectTable
	) {
		this._framework = framework;
		this._objectTable = objectTable;
		this._counter = 1;
		this._fileWindow = null;
		this._ctx = context;
		this._serializer = new JsonFileSerializer();
		this._doUpdate = false;

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
		
		_actor = new ActorEntity(this._ctx.Scene, new PoseBuilder(this._ctx.Scene), this._objectTable[442]);
		this._actor.Setup();
		this._framework.Update += this.OnFramework;
		this.Image.AttachNode(this);
		this.Border.AttachNode(this);
	}
	
	/// <summary>
	/// Framework update for our preview window, required to work 
	/// </summary>
	
	private void OnFramework(IFramework framework) {
		this._agentTryon->CharaView.Render(this._counter++);
		

		this._fileWindow = ImGuiP.FindWindowByName("###OpenFileDialog");
		if (!this._ctx.Plugin.Gui.FileDialogs.IsDialogOpen()) {
			if(this.IsVisible)
				this.Cleanup();
			this.IsVisible = false;
			this._counter = 0;
			
			return; //lets try to not overflow the games renderer
		}
		
		
		if (this.IsVisible == false) {
			//this.UpdateActorData(this._ctx.Selection.GetFirstSelected());
			
		}

		this.IsVisible = true;
		this.Position = new Vector2(this._fileWindow.Pos.X + this._fileWindow.Size.X, this._fileWindow.Pos.Y);


	}
	public void Cleanup() {
		this._ctx.Posing.ApplyReferencePose(this._actor.Pose); //reset pose
                         ////set it to first actor, will find a better way later
		this._ctx.Characters.Mcdf.RevertIfTouched(this._objectTable[442]);
		this._framework.RunOnFrameworkThread(() => {
			this._agentTryon->CharaView.Release();
			this._agentTryon->CharaView.Initialize(&this._agentTryon->AgentInterface, 2, 0);
			while (!this._agentTryon->CharaView.CharacterLoaded);
			var modelData = this._agentTryon->CharaView.ModelData;
			modelData.CopyFromCharacter((Character*)this._objectTable.LocalPlayer?.Address);
			this._agentTryon->CharaView.SetModelData(&modelData);
			this._agentTryon->CharaView.DoUpdate = true;
		});
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
	
	/// <summary>
	/// Updates actor, should be called when a new target is selected for import
	/// </summary>
	/// <param name="actor">The actor you wish to show</param>
	/// <param name="context">Editor context</param>
	public async void UpdateActorData(ActorEntity actor) {
		//

		this._framework.RunOnFrameworkThread(() => {
			this._agentTryon->CharaView.Release();
			this._agentTryon->CharaView.Initialize(&this._agentTryon->AgentInterface, 2, 0);
			var modelData = this._agentTryon->CharaView.ModelData;
			modelData.CopyFromCharacter((Character*)actor.Actor.Address);
			this._agentTryon->CharaView.SetModelData(&modelData);
			this._agentTryon->CharaView.DoUpdate = true;
		});
		
	}

/*	public ActorEntity GetFakeCharacter() {

		Character container = new Character();
		var containerPtr = &container;
		
		return new
		
	}
*/
	public void LoadMcdf(string path) {
		
		this._ctx.Characters.Mcdf.LoadAndApplyTo(path, this._objectTable[442], true);
		this.UpdateActorData(this._actor);
	}
	
	public void LoadChara(string path) {
		var content = File.ReadAllText(path);
		var file = this._serializer.Deserialize<CharaFile>(content);
		this._ctx.Characters.ApplyCharaFile(this._actor, file);
	}
	
	
	

}
