using Ktisis.Actions.Types;
using Ktisis.Editor.Expressions.Handlers;

namespace Ktisis.Editor.Expressions.Types;

public class ExpressionMemento(IExpressionController controller) : IMemento {
	// memento to capture an individual ExpressionState's blend change - combine with others as a MultipleMemento
	public required string ExpressionId { get; init; }
	public required float Initial { get; init; }
	public required float Final { get; set; }

	public void Restore() => this.Apply(this.Initial);

	public void Apply() => this.Apply(this.Final);

	private void Apply(float weight) => controller.ApplyBlend(this.ExpressionId, weight);
}
