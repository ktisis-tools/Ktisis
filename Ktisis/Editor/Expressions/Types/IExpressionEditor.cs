using System.Collections.Generic;

using Ktisis.Editor.Expressions.Data;

namespace Ktisis.Editor.Expressions.Types;

public interface IExpressionEditor {
	ActionUnitCatalog Catalog { get; }

	void EnsureNeutral();
	void CaptureNeutral(); //debug only

	float GetWeight(string id);
	void SetWeight(string id, float weight);
	void ResetWeights();

	Dictionary<string, float> BeginEdit();
	void CommitEdit(Dictionary<string, float> initial);
}
