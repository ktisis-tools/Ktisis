using System.Collections.Generic;

using Ktisis.Editor.Animation.Types;
using Ktisis.Scene.Entities.Game;
using Ktisis.Structs.Actors;

using Lumina.Excel.GeneratedSheets2;

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
	
	private const uint BattlePose = 93;
	
	// Poses

	public unsafe bool TryGetModeAndPose(out PoseModeEnum mode, out int pose) {
		var chara = actor.IsValid ? (CharacterEx*)actor.Character : null;
		if (chara == null) {
			mode = PoseModeEnum.None;
			pose = 0;
			return false;
		}
		mode = chara->EmoteController.Mode switch {
			PoseModeEnum.None => PoseModeEnum.Idle,
			var value => value
		};
		pose = chara->EmoteController.Pose;
		return true;
	}

	public int GetPoseCount(PoseModeEnum poseMode) {
		return poseMode switch {
			PoseModeEnum.Idle or PoseModeEnum.None => this.IsWeaponDrawn ? 2 : IdlePoses.Count,
			_ => StancePoses.GetValueOrDefault(poseMode, 0)
		};
	}

	public void SetPose(PoseModeEnum poseMode, byte pose = 0xFF) {
		mgr.SetPose(actor, poseMode, pose);

		if (poseMode is not (PoseModeEnum.Idle or PoseModeEnum.None))
			return;
		
		if (pose == 0)
			this.SetTimelineId(IdlePose);
		else if (this.IsWeaponDrawn)
			this.PlayEmote(BattlePose);
		else if (pose < IdlePoses.Count && IdlePoses[pose] is var eId and not 0)
			this.PlayEmote(eId);
	}
	
	// Emotes

	public void PlayEmote(Emote emote) {
		if (!this.PlayEmote(emote.RowId))
			this.SetTimelineId((ushort)emote.ActionTimeline[0].Row);
	}

	public bool PlayEmote(uint id) => mgr.PlayEmote(actor, id);
	
	// Timelines
	
	public void SetTimelineId(ushort id) {
		if (id == 0) {
			Ktisis.Log.Warning($"Attempted to set timeline ID to {id}!");
			return;
		}
		mgr.SetTimelineId(actor, id);
	}
	
	// Weapons

	public unsafe bool IsWeaponDrawn {
		get {
			var chara = actor.IsValid ? (CharacterEx*)actor.Character : null;
			return chara != null && IsWeaponDrawnFor(chara);
		}
	}

	public unsafe void ToggleWeapon() {
		var chara = actor.IsValid ? (CharacterEx*)actor.Character : null;
		if (chara == null) return;

		var isDrawn = IsWeaponDrawnFor(chara);
		this.SetTimelineId(isDrawn ? SheatheWeaponId : DrawWeaponId);
		chara->CombatFlags ^= CombatFlags.WeaponDrawn;
	}

	private unsafe static bool IsWeaponDrawnFor(CharacterEx* chara)
		=> chara->CombatFlags.HasFlag(CombatFlags.WeaponDrawn);
}
