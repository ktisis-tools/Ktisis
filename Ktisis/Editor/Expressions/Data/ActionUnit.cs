using System.Collections.Generic;

using Ktisis.Common.Utility;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Ktisis.Editor.Expressions.Data;

public class ActionUnit {
	public string Id { get; init; } = string.Empty;
	public string Label { get; init; } = string.Empty;

	/// <summary>
	/// If the bone is bi directional (-1..1) instead of (0..1)
	/// </summary>
	public bool Bidirectional { get; set; } = false;

	public bool UsePosition { get; set; } = false;

	public Dictionary<string, Transform> Bones { get; set; } = new();
}
