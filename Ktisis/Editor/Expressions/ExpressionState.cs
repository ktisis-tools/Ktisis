using System.Collections.Generic;

using Ktisis.Common.Utility;
using Ktisis.Editor.Posing.Data;

namespace Ktisis.Editor.Expressions;

public class ExpressionState {
	public PoseContainer? Neutral { get; set; }
	public Dictionary<string, float> Weights { get; } = new();

	public HashSet<string> LastTouched { get; set; } = new();

	public Dictionary<string, Transform> SolverLocal { get; set; } = new();

	public void Reset() {
		this.Neutral = null;
		this.Weights.Clear();
		this.LastTouched.Clear();
		this.SolverLocal.Clear();
	}
}
