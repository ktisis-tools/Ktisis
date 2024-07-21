using Ktisis.Structs.Actors;

using Lumina.Excel.GeneratedSheets2;

namespace Ktisis.Editor.Animation.Types;

public interface IAnimationEditor {
	public bool TryGetModeAndPose(out PoseModeEnum mode, out int pose);
	public int GetPoseCount(PoseModeEnum poseMode);
	public void SetPose(PoseModeEnum poseMode, byte pose = 0xFF);

	public void PlayEmote(Emote emote);
	public bool PlayEmote(uint id);
	
	public void SetTimelineId(ushort id);

	public bool IsWeaponDrawn { get; }
	public void ToggleWeapon();
}
