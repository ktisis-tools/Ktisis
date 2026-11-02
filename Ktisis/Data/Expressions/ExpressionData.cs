using System.Collections.Generic;

using Ktisis.Common.Utility;

namespace Ktisis.Data.Expressions;

public record ExpressionData {
	/// <summary>
	/// identifier of expression/facs unit to be represented by slider, e.g. EyeWideL
	/// </summary>
	public string Id = string.Empty;

	/// <summary>
	/// sorting order for sliders, ascending order
	/// </summary>
	public int Priority = 0;

	/// <summary>
	/// if non-null, indicates this unit should have a paired element and contains its Id
	/// (e.g. EyeWideL's Pair is EyeWideR and vice versa)
	/// </summary>
	public string? Pair = null;

	/// <summary>
	/// dictionary of boneName : Transform for each bone relevant to the expression lerp
	/// if null, expect transform contents instead in Skeletons
	/// </summary>
	public Dictionary<string, Transform>? Transforms = null;

	/// <summary>
	/// dictionary of faceId : Dictionary(boneName : Transform) for each skeleton relevant to the lerp
	/// if null, use Transforms instead
	/// if non-null, this indicates a complex lerp that requires parsing specific skeletons' transforms
	/// (e.g. BlinkL slider needs f0103's data for a face 3 character)
	/// </summary>
	public Dictionary<byte, Dictionary<string, Transform>>? Skeletons = null;
}
