using System;

using Dalamud.Plugin.Services;

using Ktisis.Editor.Animation.Handlers;
using Ktisis.Editor.Animation.Types;
using Ktisis.Editor.Context.Types;
using Ktisis.Interop.Hooking;
using Ktisis.Scene.Entities.Game;
using Ktisis.Structs.Actors;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Ktisis.Editor.Animation;

public class AnimationManager : IAnimationManager {
	private readonly IEditorContext _ctx;
	private readonly HookScope _scope;
	private readonly IDataManager _data;
	private readonly IFramework _framework;
	
	private AnimationModule? Module { get; set; }
	
	private ExcelSheet<ActionTimeline>? Timelines { get; set; }
	
	public AnimationManager(
		IEditorContext ctx,
		HookScope scope,
		IDataManager data,
		IFramework framework
	) {
		this._ctx = ctx;
		this._scope = scope;
		this._data = data;
		this._framework = framework;
	}
	
	// Initialization
	
	public void Initialize() {
		Ktisis.Log.Verbose("Initializing character manager...");

		try {
			this.Module = this._scope.Create<AnimationModule>();
			this.Module.Initialize();
			this.Module.EnableAll();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to initialize animation module:\n{err}");
		}
		
		this.Timelines = this._data.GetExcelSheet<ActionTimeline>();
		this.Module.PositionLockEnabled = this._ctx.Config.Editor.InitPosLock;
	}
	
	// Controls
	
	public bool SpeedControlEnabled {
		get => this.Module?.SpeedControlEnabled ?? false;
		set {
			if (this.Module != null)
				this.Module.SpeedControlEnabled = value;
		}
	}

	public bool PositionLockEnabled {
		get => this.Module?.PositionLockEnabled ?? false;
		set {
			if (this.Module != null)
				this.Module.PositionLockEnabled = value;
		}
	}
	
	// Editors

	public IAnimationEditor GetAnimationEditor(ActorEntity actor) => new AnimationEditor(this, _ctx, actor);
	
	// Pose control

	public void SetPose(ActorEntity actor, PoseModeEnum poseMode, byte pose = byte.MaxValue) {
		this._framework.RunOnFrameworkThread(() => {
			this.Module?.SetPose(actor, poseMode, pose);
		});
	}
	
	// Animation & timeline wrappers

	public unsafe bool PlayEmote(ActorEntity actor, uint id) {
		var chara = (CharacterEx*)actor.Character;
		if (chara == null) return false;
		chara->Animation.Timeline.ActionTimelineId = 0;
		chara->EmoteController.IsForceDefaultPose = false;
		return this.Module!.PlayEmote(&chara->EmoteController, (nint)id, 0, 0);
	}

	public unsafe bool PlayTimeline(ActorEntity actor, uint id) {
		var timeline = this.Timelines?.GetRow(id);
		if (timeline == null) return false;

		var chara = actor.IsValid ? (CharacterEx*)actor.Character : null;
		if (chara == null) return false;
		
		chara->Animation.Timeline.ActionTimelineId = 0;

		if (timeline.Value.Pause) {
			chara->Mode = 3;
			chara->EmoteMode = 0;
		} else if (chara->Mode == 3 && chara->EmoteMode == 0) {
			chara->Mode = 1;
		}

		return this.Module != null
			&& this.Module.SetTimelineId(&chara->Animation.Timeline, (ushort)id, nint.Zero);
	}
	
	public unsafe void SetTimelineSpeed(ActorEntity actor, uint slot, float speed) {
		var chara = actor.IsValid ? (CharacterEx*)actor.Character : null;
		if (chara == null) return;

		this.Module?.SetTimelineSpeed(&chara->Animation.Timeline, slot, speed);
	}

	public unsafe void ResetTimelineSpeeds(ActorEntity actor) {
		var chara = actor.IsValid ? (CharacterEx*)actor.Character : null;
		if (chara == null) return;

		foreach (var slot in Enum.GetValues<TimelineSlot>())
			this.Module?.SetTimelineSpeed(&chara->Animation.Timeline, (uint)slot, 1.0f);
	}
}
