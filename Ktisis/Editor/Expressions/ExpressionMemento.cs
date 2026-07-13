using System;
using System.Collections.Generic;

using Ktisis.Actions.Types;

namespace Ktisis.Editor.Expressions;

public class ExpressionMemento(ExpressionState state, Action applyBlend) : IMemento {
	public required IReadOnlyDictionary<string, float> Initial { get; init; }
	public required IReadOnlyDictionary<string, float> Final { get; init; }

	public void Restore() => this.Apply(this.Initial);
	public void Apply() => this.Apply(this.Final);

	private void Apply(IReadOnlyDictionary<string, float> weights) {
		state.Weights.Clear();
		foreach (var (k, v) in weights)
			state.Weights[k] = v;
		applyBlend();
	}
}
