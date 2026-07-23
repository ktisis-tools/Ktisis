using System.Collections.Generic;

using Ktisis.Common.Utility;
using Ktisis.Data.Expressions;

namespace Ktisis.Editor.Expressions.State;

public record ExpressionState {
	public float Weight;
	public readonly Dictionary<string, Transform> Blend = [];
	public readonly Dictionary<string, Transform> Delta = [];
	
	public required ExpressionData Data;

	public void Reset() {
		this.Weight = 0.0f;
		this.PrepareBlend();
	}

	public void PrepareBlend() {
		foreach (var bone in this.Data.Transforms.Keys) {
			this.Blend[bone] = new();
			this.Delta[bone] = new();
		}
	}
}
