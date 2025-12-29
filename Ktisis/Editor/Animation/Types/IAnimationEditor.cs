using Ktisis.Editor.Animation.Game;
using Ktisis.Structs.Actors;

using FFXIVClientStructs.Havok.Animation.Playback.Control.Default;

namespace Ktisis.Editor.Animation.Types;

public interface IAnimationEditor {
	public bool SpeedControlEnabled { get; set; }
	public bool PositionLockEnabled { get; set; }
	public bool Posing { get; }
	
	public bool TryGetModeAndPose(out PoseModeEnum mode, out int pose);
	public int GetPoseCount(PoseModeEnum poseMode);
	public void SetPose(PoseModeEnum poseMode, byte pose = 0xFF);

	public void PlayAnimation(GameAnimation animation, bool playStart = true);
	
	public void PlayTimeline(uint id);
	public AnimationTimeline GetTimeline();
	public void SetForceTimeline(ushort id);
	public void SetTimelineSpeed(uint slot, float speed);
	public void ResetTimelineSpeeds();
	public void DoPoseExpression(uint id);
	public unsafe hkaDefaultAnimationControl* GetHkaControl(int index);
	public unsafe float? GetHkaDuration(hkaDefaultAnimationControl* control);
	public unsafe float? GetHkaLocalTime(hkaDefaultAnimationControl* control);
	public unsafe void SetHkaLocalTime(hkaDefaultAnimationControl* control, float time);

	public bool IsWeaponDrawn { get; }
	public void ToggleWeapon();
}
