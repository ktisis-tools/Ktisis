using System.Collections.Generic;

using Ktisis.Editor.Posing.Data;

namespace Ktisis.Editor.Expressions;

// Per-actor expression state. Neutral is the captured baseline face (model-space
// transforms keyed by bone name) that AU deltas are blended on top of. Weights
// maps AU id -> slider value.
public class ExpressionState {
	public PoseContainer? Neutral { get; set; }
	public Dictionary<string, float> Weights { get; } = new();

	// Bone names the last ApplyBlend actually moved (driven AU bones + their
	// propagation descendants). The next pass restores exactly these to neutral,
	// so bones the solver never touches (manually-posed eyes, lips, etc.) are left
	// alone instead of being clobbered.
	public HashSet<string> LastTouched { get; set; } = new();
}
