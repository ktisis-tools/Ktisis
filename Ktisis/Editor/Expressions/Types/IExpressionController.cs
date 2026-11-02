using System.Collections.Generic;

using Ktisis.Editor.Expressions.State;
using Ktisis.Scene.Decor;

namespace Ktisis.Editor.Expressions.Types;

public interface IExpressionController {
	public ushort RaceSexId { get; }
	public int Count { get; }

	public IReadOnlyDictionary<string, ExpressionState> GetExpressions();

	public void Setup(ISkeleton skeleton);

	public void Update();

	public void Load(ushort raceSexId, byte faceId);
	public void Unload();

	public void ResetBlendState();
	public List<ExpressionMemento> ResetBlendWeights(bool weightsOnly = true);

	public void ApplyBlend(string id, float weight);
	public void SetWeight(string id, float weight);

	public void Destroy();
}
