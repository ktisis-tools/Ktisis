using System.Collections.Generic;
using System.Linq;

using Ktisis.Common.Utility;
using Ktisis.Data.Expressions;

namespace Ktisis.Editor.Expressions.State;

public record ExpressionState {
	public float Weight;
	public byte Face;
	public readonly Dictionary<string, Transform> Blend = [];
	
	public required ExpressionData Data;

	public void Reset() {
		this.Weight = 0.0f;
		this.PrepareBlend();
	}

	public void PrepareBlend() {
		// if Skeletons is not-null (ex. in case of BlinkL/BlinkR), make blends based on a specific face or fallback to first
		if (this.Data.Skeletons is not null) {
			if (this.Data.Skeletons.TryGetValue(this.Face, out var skeleton))
				foreach (var bone in skeleton.Keys)
					this.Blend[bone] = new();
			else
				foreach (var bone in this.Data.Skeletons[this.Data.Skeletons.Keys.First()].Keys)
					this.Blend[bone] = new();
			return;
		}

		// if Transforms is not null (all others)
		if (this.Data.Transforms is not null)
			foreach (var bone in this.Data.Transforms.Keys)
				this.Blend[bone] = new();
	}
}
