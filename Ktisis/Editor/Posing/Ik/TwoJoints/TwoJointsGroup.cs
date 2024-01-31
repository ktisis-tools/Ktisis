using System.Numerics;

using Ktisis.Editor.Posing.Ik.Types;

namespace Ktisis.Editor.Posing.Ik.TwoJoints;

public enum TwoJointsMode {
	Fixed,
	Relative
}

public record TwoJointsGroup : IIkGroup {
	public bool IsEnabled { get; set; }
	public uint SkeletonId { get; set; }
	
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
}
