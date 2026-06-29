using Ktisis.Editor.Expressions.Data;
using Ktisis.Editor.Posing.Data;

namespace Ktisis.Editor.Expressions.Types;

public interface IExpressionEditor {
	ActionUnitCatalog Catalog { get; }

	void EnsureNeutral();
	void CaptureNeutral(); //debug only

	float GetWeight(string id);
	void SetWeight(string id, float weight);
	void ResetWeights();


	PoseContainer BeginEdit();
	void CommitEdit(PoseContainer initial);
}
