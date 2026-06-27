using System.Collections.Generic;

using Ktisis.Editor.Expressions.Data;
using Ktisis.Editor.Posing.Data;

namespace Ktisis.Editor.Expressions.Types;

public interface IExpressionEditor {
	ActionUnitCatalog Catalog { get; }

	bool HasNeutral { get; }

	void EnsureNeutral();
	void CaptureNeutral();

	float GetWeight(string id);
	void SetWeight(string id, float weight);
	void ResetWeights();

	ActionUnit CaptureCurrentAsAu(string id, string label);

	void RemoveUnit(string id);

	void LoadState(IReadOnlyDictionary<string, float>? weights, PoseContainer? neutral);

	PoseContainer BeginEdit();
	void CommitEdit(PoseContainer initial);
}
