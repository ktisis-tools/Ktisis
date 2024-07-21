using System;
using System.Numerics;

using Dalamud.Plugin.Services;

using Ktisis.Editor.Animation.Handlers;
using Ktisis.Editor.Animation.Types;
using Ktisis.Editor.Context.Types;
using Ktisis.Interop.Hooking;
using Ktisis.Scene.Entities.Game;
using Ktisis.Structs.Actors;

namespace Ktisis.Editor.Animation;

public class AnimationManager : IAnimationManager {
	private readonly IEditorContext _ctx;
	private readonly HookScope _scope;
	private readonly IFramework _framework;
	
	public AnimationManager(
		IEditorContext ctx,
		HookScope scope,
		IFramework framework
	) {
		this._ctx = ctx;
		this._scope = scope;
		this._framework = framework;
	}
	
	// Initialization
	
	private AnimationModule? Module { get; set; }
	public AnimationModule __module__ => this.Module!;
	
	public void Initialize() {
		Ktisis.Log.Verbose("Initializing character manager...");

		try {
			this.Module = this._scope.Create<AnimationModule>();
			this.Module.Initialize();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to initialize animation module:\n{err}");
		}
	}
	
	// Editors

	public IAnimationEditor GetAnimationEditor(ActorEntity actor) => new AnimationEditor(this, actor);
	
	// Pose control

	public void SetPose(ActorEntity actor, PoseModeEnum poseMode, byte pose = byte.MaxValue) {
		this._framework.RunOnFrameworkThread(() => {
			this.Module?.SetPose(actor, poseMode, pose);
		});
	}
	
	// Wrappers

	public unsafe bool SetTimelineId(ActorEntity actor, ushort id) {
		var timeline = this.GetTimeline(actor);

		if (id == 0) {
			var chara = (CharacterEx*)actor.Character;
			this.Module!.SchedulerAction(&chara->Animation, 0x1865, 0x1866, (nint)0xDA082FD870);
			return true;
		}
		
		return timeline != null
			&& this.Module != null
			&& this.Module.SetTimelineId(timeline, id, nint.Zero);
	}

	public unsafe bool PlayEmote(ActorEntity actor, uint id) {
		var chara = (CharacterEx*)actor.Character;
		return chara != null && this.Module!.PlayEmote(&chara->EmoteController, (nint)id, 0, 0);
	}

	private unsafe AnimationTimeline* GetTimeline(ActorEntity actor) {
		var chara = actor.IsValid ? (CharacterEx*)actor.Character : null;
		return chara != null ? &chara->Animation.Timeline : null;
	}
}
