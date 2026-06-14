using System.Collections.Generic;

using Ktisis.Editor.Expressions.Data;
using Ktisis.Editor.Posing.Data;

namespace Ktisis.Editor.Expressions.Types;

public interface IExpressionEditor {
	ActionUnitCatalog Catalog { get; }

	bool HasNeutral { get; }

	// Captures the current face as the neutral baseline (only if not already set).
	void EnsureNeutral();
	// Forces a fresh neutral capture from the current face.
	void CaptureNeutral();
	// Discards the captured neutral so the next edit recaptures (e.g. after a
	// face partial skeleton rebuild).
	void InvalidateNeutral();

	float GetWeight(string id);
	void SetWeight(string id, float weight);
	void ResetWeights();

	// Recomputes the blended face from neutral + weights and writes it to the
	// frozen model pose.
	void ApplyBlend();

	// Captures the current (manually posed) face as a new AU delta relative to
	// the neutral baseline.
	ActionUnit CaptureCurrentAsAu(string id, string label);

	// Resets the AU's bones to neutral, then deletes the AU from the catalog.
	void RemoveUnit(string id);

	// Seeds slider/neutral state from a loaded pose file.
	void LoadState(IReadOnlyDictionary<string, float>? weights, PoseContainer? neutral);
	// Exports the current slider weights for saving into a pose file.
	Dictionary<string, float> ExportWeights();

	// Undo helpers: capture a snapshot before a slider drag, commit a memento after.
	PoseContainer BeginEdit();
	void CommitEdit(PoseContainer initial);
}
