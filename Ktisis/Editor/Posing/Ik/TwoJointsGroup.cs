using System.Numerics;

namespace Ktisis.Editor.Posing.Ik;

public enum TwoJointsMode {
	Fixed,
	Relative
}

public record TwoJointsGroup {
	public bool IsEnabled;
	
	public TwoJointsMode Mode = TwoJointsMode.Relative;
	
	public short FirstBoneIndex = -1;
	public short FirstTwistIndex = -1;
	public short SecondBoneIndex = 1;
	public short SecondTwistIndex = 1;
	public short EndBoneIndex = -1;

	public Vector3 TargetPosition = Vector3.Zero;
	public Quaternion TargetRotation = Quaternion.Identity;
	
	public uint SkeletonId;
}
