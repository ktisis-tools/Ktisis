using System.Collections.Generic;

using Ktisis.Editor.Posing.Data;

namespace Ktisis.Editor.Expressions;

// Per-actor expression state. Neutral is the captured baseline face (model-space
// transforms keyed by bone name) that AU deltas are blended on top of. Weights
// maps AU id -> slider value.
public class ExpressionState {
	public PoseContainer? Neutral { get; set; }
	public Dictionary<string, float> Weights { get; } = new();
}
