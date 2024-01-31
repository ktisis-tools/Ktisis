using System.Numerics;

using Ktisis.Editor.Posing.Ik.Types;

namespace Ktisis.Editor.Posing.Ik.Ccd;

public class CcdGroup : IIkGroup {
	public bool IsEnabled { get; set; }
	public uint SkeletonId { get; set; }

	public short StartBoneIndex = -1;
	public short EndBoneIndex = -1;

	public int Iterations = 8;
	public float Gain = 0.5f;

	public Vector3 TargetPosition;
}
