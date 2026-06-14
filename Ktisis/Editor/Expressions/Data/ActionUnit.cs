using System.Collections.Generic;

using Ktisis.Common.Utility;

namespace Ktisis.Editor.Expressions.Data;

// A FACS-style Action Unit: a named, weighted facial-expression control.
// Each entry in Bones is the FULL-WEIGHT delta (relative to the neutral face)
// applied to that bone. Rotation is the primary channel; Position is optional
// (used for translation-driven units such as jaw open). Scale is ignored.
public class ActionUnit {
	public string Id { get; set; } = string.Empty;
	public string Label { get; set; } = string.Empty;

	// When true the slider ranges -1..1, where a negative weight applies the
	// inverse of the stored delta (e.g. brow up / brow down on one control).
	public bool Bidirectional { get; set; } = false;

	// When true the Position channel of each bone delta is blended in addition
	// to rotation. Most expressions only need rotation.
	public bool UsePosition { get; set; } = false;

	// Bone name -> full-weight delta relative to neutral.
	public Dictionary<string, Transform> Bones { get; set; } = new();
}
