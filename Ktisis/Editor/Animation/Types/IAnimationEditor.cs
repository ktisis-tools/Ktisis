using Ktisis.Editor.Animation.Game;
using Ktisis.Structs.Actors;

namespace Ktisis.Editor.Animation.Types;

public interface IAnimationEditor {
	public bool TryGetModeAndPose(out PoseModeEnum mode, out int pose);
	public int GetPoseCount(PoseModeEnum poseMode);
	public void SetPose(PoseModeEnum poseMode, byte pose = 0xFF);

	public void PlayAnimation(GameAnimation animation, bool playStart = true);
	
	public void PlayTimeline(uint id);
	public AnimationTimeline GetTimeline();
	public void SetForceTimeline(ushort id);

	public bool IsWeaponDrawn { get; }
	public void ToggleWeapon();
}
