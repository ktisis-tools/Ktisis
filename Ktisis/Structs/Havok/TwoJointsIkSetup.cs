using System.Numerics;
using System.Runtime.InteropServices;

namespace Ktisis.Structs.Havok;

[StructLayout(LayoutKind.Explicit)]
public struct TwoJointsIkSetup {
	/// The id of the first joint (ball-socket : thigh, shoulder)
	[FieldOffset(0)] public short m_firstJointIdx;

	/// The id of the second joint (hinge : knee, elbow)
	[FieldOffset(2)] public short m_secondJointIdx;

	/// The id of the end bone (usually heel, hand, toe)
	[FieldOffset(4)] public short m_endBoneIdx;

	/// The id of the twist bone corresponding to the first joint (optional)
	[FieldOffset(6)] public short m_firstJointTwistIdx;

	/// The id of the twist bone corresponding to the second joint (optional)
	[FieldOffset(8)] public short m_secondJointTwistIdx;

	/// The hinge axis for the second joint, in local space. Positive rotations (using right hand rule) around this
	/// axis should extend the limb.
	[FieldOffset(16)] public Vector4 m_hingeAxisLS;

	/// Limit the hinge angle (to avoid knee/elbow artifacts). Default is -1 (180 deg). 
	[FieldOffset(32)] public float m_cosineMaxHingeAngle;

	/// Limit the hinge angle (to avoid knee/elbow artifacts). Default is 1 (0 deg). 
	[FieldOffset(36)] public float m_cosineMinHingeAngle;

	/// Gain of the Ik applied to the first joint (from 0 to 1). You can use this to transition smoothly from/to ik-fixed poses
	[FieldOffset(40)] public float m_firstJointIkGain;

	/// Gain of the Ik applied to the second joint (from 0 to 1). You can use this to transition smoothly from/to ik-fixed poses
	[FieldOffset(44)] public float m_secondJointIkGain;

	/// Gain of the Ik applied to the end joint (from 0 to 1). You can use this to transition smoothly from/to ik-fixed poses
	/// Only has an effect if m_enforceEndRotation is true
	[FieldOffset(48)] public float m_endJointIkGain;

	/// The target position for the end bone, in model space
	[FieldOffset(64)] public Vector4 m_endTargetMS;

	/// The target rotation for the end bone in model space
	[FieldOffset(80)] public Quaternion m_endTargetRotationMS;

	/// The offset of the end effector in the local space of the end bone
	[FieldOffset(96)] public Vector4 m_endBoneOffsetLS;
	
	/// The rotation offset of the end effector in the local space of the end bone
	[FieldOffset(112)] public Quaternion m_endBoneRotationOffsetLS;
	
	/// Set to true if the position of the end effector is to be solved for
	[FieldOffset(128)] public bool m_enforceEndPosition;

	/// Set to true if the rotation of the end effector is to be solved for
	[FieldOffset(129)] public bool m_enforceEndRotation;
}
