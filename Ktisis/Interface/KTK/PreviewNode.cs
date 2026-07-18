using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
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
using KamiToolKit.BaseTypes;
using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Extensions;
using KamiToolKit.UiOverlay;

using Ktisis.Data.Files;
using Ktisis.Data.Json;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Data;
using Ktisis.Editor.Posing.Ik;
using Ktisis.Editor.Posing.Types;
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
	private readonly ImageNode ImageBacking;
	private readonly NineGridNode Border;
	private readonly NodeBase Buttons;

	private uint _counter;
	private ActorEntity _actor;
	private ActorEntity _target;
	private bool needsToApplyCollection = true;

	private readonly RenderTargetManager* _renderTargetManager;
	private readonly AgentInspect* _agentInspect;
	private ImGuiWindowPtr _fileWindow;

	private readonly IFramework _framework;
	private readonly IObjectTable _objectTable;
	private readonly IEditorContext _ctx;
	private readonly JsonFileSerializer _serializer;

	//Stuff for auto dialog application
	private PoseFile? _currentPose;
	private PoseTransforms _currentTransforms;
	private PoseMode _currentMode;
	private bool _currentEars;
	private bool _currentAnchor;
	private bool _currentBones;
	private bool _currentChildren;

	public PreviewNode(
		IEditorContext context,
		IFramework framework,
		IObjectTable objectTable,
		ActorEntity target
	) {
		if (target.GetHuman() == null)
			return;

		this._target = target;
		this._currentPose = null;
		this._framework = framework;
		this._objectTable = objectTable;
		this._counter = 1;
		this._fileWindow = null;
		this._ctx = context;
		this._serializer = new JsonFileSerializer();

		
		this._renderTargetManager = RenderTargetManager.Instance();
		this._agentInspect = AgentInspect.Instance(); // idk why this was below the eval before?

		this.Image = new ImageNode() {
			Size = new Vector2(192.0f, 320.0f),
			Position = new Vector2(4, 3),
			ImageNodeFlags = (ImageNodeFlags)0x8C,
			WrapMode = WrapMode.Tile
		};
		this.ImageBacking = new ImageNode() {
			Size = new Vector2(192.0f, 320.0f),
			Position = new Vector2(4, 3),
			ImageNodeFlags = ImageNodeFlags.AutoFit,
			WrapMode = WrapMode.Tile
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


		
		var part = this.Image.AddPart(new Part { 		
			Height = 320,
			Width = 192,});
		part->LoadTexture(this._renderTargetManager->CharaViewTextures[1]);
		this._renderTargetManager->CharaViewTextures[1].Value->IncRef();
		
		var bgpart = this.ImageBacking.AddPart(new Part { 		
			Height = 320,
			Width = 192,});
		bgpart->LoadTexture("ui/common/characterbg_hr1.tex");
		

		this._framework.RunOnFrameworkThread(() => {
			this._agentInspect->CharaView.Initialize(&this._agentInspect->AgentInterface, 1, 0);
			this._agentInspect->CharaView.ModelData.CopyFromCharacter((Character*)target.Actor.Address);
		});

		this.Buttons = this.SetupButtons();
		
		this._actor = new ActorEntity(this._ctx.Scene, new PoseBuilder(this._ctx.Scene), this._objectTable[441]);
		this._actor.Setup();
		this._framework.Update += this.OnFramework;
		
		this.ImageBacking.AttachNode(this);
		this.Image.AttachNode(this);
		this.Border.AttachNode(this);
		this.Buttons.AttachNode(this);
		
		this._agentInspect->CharaView.Update(this._counter, this._agentInspect->CharaView.GetCharacter());
	}

	/// <summary>
	/// Framework update for our preview window, required to work 
	/// </summary>
	private void OnFramework(IFramework framework) {
		/*if (this.needsToApplyCollection && this._objectTable[441]?.Address != null) {
			var ipc = this._ctx.Plugin.Ipc.GetPenumbraIpc();
			var collection = ipc.GetCollectionForObject(this._target.Actor);
			ipc.SetCollectionForObject(this._objectTable[441], collection.Id);
			this.needsToApplyCollection = false;
		}*/

		this._agentInspect->CharaView.Update(this._counter, this._actor.Character);
		this._agentInspect->CharaView.Render(this._counter++);

		this._fileWindow = ImGuiP.FindWindowByName("###OpenFileDialog");
		if (!this._ctx.Plugin.Gui.FileDialogs.IsDialogOpen()) {
			this.Cleanup();
			return; // lets try to not overflow the games renderer
		}

		this.IsVisible = true;
		this.Position = new Vector2(this._fileWindow.Pos.X + this._fileWindow.Size.X, this._fileWindow.Pos.Y);

		if (this.NeedsUpdate() && this._currentPose != null) {
			this._ctx.Posing.ApplyReferencePose(_actor.Pose);
			if (this._ctx.Config.File.ImportPoseSelectedBones)
				this.CopySelectedBones();

			if(!this._actor.Pose.HasDTFace())
				this._actor.Pose.Update();
			
			this.ApplyPose();
			this.UpdateLocals();
		}
	}

	private NodeBase SetupButtons() {
		NodeBase buttonsNode = new ResNode() {
			Size = new Vector2(168.0f, 32.0f),
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
			Icon = CircleButtonIcon.RightArrow,
			Position = new Vector2(64f, 0f),
			Size = new Vector2(32.0f, 32.0f),
			Scale = new Vector2(-1f, 1f),
		};
		buttonLeft.OnClick = () => {
			this.MoveCamera(0, -50f);
		};

		ButtonBase buttonRight = new CircleButtonNode() {
			Icon = CircleButtonIcon.RightArrow,
			Position = new Vector2(64f, 0f),
			Size = new Vector2(32.0f, 32.0f),
		};
		buttonRight.OnClick = () => {
			this.MoveCamera(0, 50f);
		};

		ButtonBase buttonReset = new CircleButtonNode() {
			Icon = CircleButtonIcon.Undo,
			Position = new Vector2(148f, 0f),
			Size = new Vector2(32.0f, 32.0f),
		};
		buttonReset.OnClick = this.ResetCamera;

		buttonLeft.AttachNode(buttonsNode);
		buttonRight.AttachNode(buttonsNode);
		buttonReset.AttachNode(buttonsNode);
		return buttonsNode;
	}

	private void MoveCamera(float pitch, float yaw) {
		this._agentInspect->CharaView.SetCameraYawAndPitch(yaw, pitch);
	}

	/// <summary>
	/// Resets the camera position
	/// </summary>
	private void ResetCamera() {
		this._agentInspect->CharaView.ResetPositions();
		//this._agentInspect->CharaView.SetCameraYawAndPitch(this._yaw * -1, this._pitch * -1);
	}

	/// <summary>
	/// Poses the actor in the preview window
	/// </summary>
	/// <param name="path">Path of the pose file</param>
	public void PoseActor(string path) {
		var content = File.ReadAllText(path);
		if (Path.GetExtension(path).Equals(".cmp")) content = LegacyPoseHelpers.ConvertLegacyPose(content);
		this._currentPose = this._serializer.Deserialize<PoseFile>(content);

		
		this._ctx.Posing.ApplyReferencePose(_actor.Pose);
		if (this._ctx.Config.File.ImportPoseSelectedBones)
			this.CopySelectedBones();

		if(!this._actor.Pose.HasDTFace())
			this._actor.Pose.Update();
		
		this.ApplyPose();
		this.UpdateLocals();
	}
	
	private void ApplyPose() => this._ctx.Posing.ApplyPoseFile(_actor.Pose,
		this._currentPose,
		transforms: this._ctx.Config.File.ImportPoseTransforms,
		modes: PoseMode.Body,
		anchorGroups: this._ctx.Config.File.AnchorPoseSelectedBones,
		selectedBones: (this._target.Pose.Recurse().Any(b => b.IsSelected) ? this._ctx.Config.File.ImportPoseSelectedBones : false),
		includeDescendants: this._ctx.Config.File.SelectedBonesIncludeDescendants,
		excludeEars: this._ctx.Config.File.ExcludePoseEarBones
	);

	private void UpdateLocals() {
		this._currentAnchor = this._ctx.Config.File.AnchorPoseSelectedBones;
		this._currentBones = this._ctx.Config.File.ImportPoseSelectedBones;
		this._currentAnchor = this._ctx.Config.File.AnchorPoseSelectedBones;
		this._currentChildren = this._ctx.Config.File.SelectedBonesIncludeDescendants;
		this._currentEars = this._ctx.Config.File.ExcludePoseEarBones;
		this._currentMode = this._ctx.Config.File.ImportPoseModes;
		this._currentTransforms = this._ctx.Config.File.ImportPoseTransforms;
	}

	private void CopySelectedBones() {
		//clear currently selected bones for the preview actor
		var previewBones = this._actor.Pose.Recurse()
			.Prepend(this._actor)
			.Where(entity => entity is SkeletonNode { IsSelected: true })
			.Cast<SkeletonNode>();
		foreach (var bone in previewBones) {
			this._ctx.Selection.Unselect(bone);
		}
		//this._actor.Pose.Update();
		//get selected bones in target
		var selected = this._target.Pose.Recurse()
			.Prepend(this._target)
			.Where(entity => entity is SkeletonNode { IsSelected: true })
			.Cast<SkeletonNode>();
		var selectedBones =  this.GetBoneSelectionFrom(selected, true).Distinct();
		if (this._ctx.Config.File.SelectedBonesIncludeDescendants)
			selectedBones = this._target.Pose.ExpandToDescendants(selectedBones);
		
		//copy selected bones
		foreach (var bone in selectedBones) {
			var node = this._actor.Pose.FindBoneByName(bone.Name);
			if(node != null)
				node.Select();
		}

	}
	private IEnumerable<PartialBoneInfo> GetBoneSelectionFrom(IEnumerable<SkeletonNode> nodes, bool all = true) {
		foreach (var node in nodes) {
			switch (node) {
				case BoneNode bone:
					yield return bone.Info;
					continue;
				case SkeletonGroup group:
					foreach (var bone in this.GetBoneSelectionFrom(all ? group.GetAllBones() : group.GetIndividualBones()))
						yield return bone;
					continue;
				default:
					continue;
			}
		}
	}

	private bool NeedsUpdate() => this._currentAnchor != this._ctx.Config.File.AnchorPoseSelectedBones ||
		this._currentBones != this._ctx.Config.File.ImportPoseSelectedBones ||
		this._currentAnchor != this._ctx.Config.File.AnchorPoseSelectedBones ||
		this._currentChildren != this._ctx.Config.File.SelectedBonesIncludeDescendants ||
		this._currentEars != this._ctx.Config.File.ExcludePoseEarBones ||
		this._currentMode != this._ctx.Config.File.ImportPoseModes ||
		this._currentTransforms != this._ctx.Config.File.ImportPoseTransforms;

	public void Cleanup() {
		this._framework.Update -= this.OnFramework;
		this._agentInspect->CharaView.Release();
		this.Dispose();
	}
}
