using System.Collections.Generic;

namespace Ktisis.Data.Config.Bones;

public enum TwoJointsType {
	None = 0,
	Arm = 1,
	Leg = 2
}

public class TwoJointsGroupParams {
	public TwoJointsType Type = TwoJointsType.None;
	
	public List<string> FirstBone = [];
	public List<string> FirstTwist = [];
	public List<string> SecondBone = [];
	public List<string> SecondTwist = [];
	public List<string> EndBone = [];
}
