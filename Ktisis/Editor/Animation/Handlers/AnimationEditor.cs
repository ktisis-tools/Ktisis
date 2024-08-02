using System.Collections.Generic;

using Ktisis.Editor.Animation.Game;
using Ktisis.Editor.Animation.Types;
using Ktisis.Scene.Entities.Game;
using Ktisis.Structs.Actors;

namespace Ktisis.Editor.Animation.Handlers;

public class AnimationEditor(
	IAnimationManager mgr,
	ActorEntity actor
) : IAnimationEditor {
	private readonly static List<uint> IdlePoses = [ 0, 91, 92, 107, 108, 218, 219 ];
	private readonly static Dictionary<PoseModeEnum, int> StancePoses = new() {
		{ PoseModeEnum.SitGround, 4 },
		{ PoseModeEnum.SitChair, 5 },
		{ PoseModeEnum.Sleeping, 3 }
	};

	private const ushort IdlePose = 3;
	private const ushort DrawWeaponId = 1;
	private const ushort SheatheWeaponId = 2;

	private const uint BattleIdle = 34;
	private const uint BattlePose = 93;
	
	// Character

	private unsafe CharacterEx* GetChara() => actor.IsValid ? (CharacterEx*)actor.Character : null;
	
	// Speed control

	public bool SpeedControlEnabled {
		get => mgr.SpeedControlEnabled;
		set => mgr.SpeedControlEnabled = value;
	}

	public void SetTimelineSpeed(uint slot, float speed) => mgr.SetTimelineSpeed(actor, slot, speed);
	
	// Poses

	public unsafe bool TryGetModeAndPose(out PoseModeEnum mode, out int pose) {
		var chara = actor.IsValid ? (CharacterEx*)actor.Character : null;
		if (chara == null) {
			mode = PoseModeEnum.None;
			pose = 0;
			return false;
		}
		mode = chara->EmoteMode switch {
			EmoteModeEnum.SitGround => PoseModeEnum.SitGround,
			EmoteModeEnum.SitChair => PoseModeEnum.SitChair,
			EmoteModeEnum.Sleeping => PoseModeEnum.Sleeping,
			_ => chara->EmoteController.Mode switch {
				PoseModeEnum.None => PoseModeEnum.Idle,
				var value => value
			}
		};
		pose = chara->EmoteController.Pose;
		return true;
	}

	public int GetPoseCount(PoseModeEnum poseMode) {
		return poseMode switch {
			PoseModeEnum.Idle or PoseModeEnum.None => this.IsWeaponDrawn ? 2 : IdlePoses.Count,
			_ => StancePoses.GetValueOrDefault(poseMode, 1)
		};
	}

	public void SetPose(PoseModeEnum poseMode, byte pose = 0xFF) {
		mgr.SetPose(actor, poseMode, pose);

		if (poseMode is not (PoseModeEnum.Idle or PoseModeEnum.None))
			return;
		
		if (pose == 0)
			mgr.PlayTimeline(actor, this.IsWeaponDrawn ? BattleIdle : IdlePose);
		else if (this.IsWeaponDrawn)
			mgr.PlayEmote(actor, BattlePose);
		else if (pose < IdlePoses.Count && IdlePoses[pose] is var eId and not 0)
			mgr.PlayEmote(actor, eId);
	}
	
	// Animations

	public void PlayAnimation(GameAnimation animation, bool playStart = true) {
		switch (animation) {
			case EmoteAnimation { Index: 0 } emote when playStart:
				if (mgr.PlayEmote(actor, emote.EmoteId))
					break;
				goto default;
			default:
				mgr.PlayTimeline(actor, animation.TimelineId);
				break;
		}
	}
	
	public void PlayTimeline(uint id) => mgr.PlayTimeline(actor, id);

	public unsafe AnimationTimeline GetTimeline() {
		var chara = this.GetChara();
		return chara != null ? chara->Animation.Timeline : default;
	}

	public unsafe void SetForceTimeline(ushort id) {
		var chara = this.GetChara();
		if (chara == null) return;

		chara->Animation.Timeline.ActionTimelineId = id;
	}
	
	// Weapons

	public unsafe bool IsWeaponDrawn {
		get {
			var chara = this.GetChara();
			return chara != null && IsWeaponDrawnFor(chara);
		}
	}

	public unsafe void ToggleWeapon() {
		var chara = this.GetChara();
		if (chara == null) return;

		var isDrawn = IsWeaponDrawnFor(chara);
		this.PlayTimeline(isDrawn ? SheatheWeaponId : DrawWeaponId);
		chara->CombatFlags ^= CombatFlags.WeaponDrawn;
	}

	private unsafe static bool IsWeaponDrawnFor(CharacterEx* chara)
		=> chara->CombatFlags.HasFlag(CombatFlags.WeaponDrawn);
}
