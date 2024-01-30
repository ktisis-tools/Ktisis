using System.Numerics;

namespace Ktisis.Editor.Posing.Ik;

public enum TwoJointsMode {
	Fixed,
	Relative
}

public record TwoJointsGroup {
	public bool IsEnabled;
	
	public TwoJointsMode Mode = TwoJointsMode.Fixed;
	
	public short FirstBoneIndex = -1;
	public short FirstTwistIndex = -1;
	public short SecondBoneIndex = 1;
	public short SecondTwistIndex = 1;
	public short EndBoneIndex = -1;

	public float FirstBoneGain = 1.0f;
	public float SecondBoneGain = 1.0f;
	public float EndBoneGain = 1.0f;

	public float MaxHingeAngle = -1.0f;
	public float MinHingeAngle = 1.0f;
	public Vector3 HingeAxis = new(0.0f, 0.0f, 1.0f);
	
	public bool EnforcePosition = true;
	public Vector3 TargetPosition = Vector3.Zero;

	public bool EnforceRotation = true;
	public Quaternion TargetRotation = Quaternion.Identity;
	
	public uint SkeletonId;
}
