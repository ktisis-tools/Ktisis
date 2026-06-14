using System.Collections.Generic;
using System.Linq;

using Ktisis.Editor.Expressions.Data;

namespace Ktisis.Editor.Expressions;

// A loaded AU catalog for one race+gender+clan variant, plus its affected-bone set.
// Captured/removed AUs mutate this library (shared by all actors of that variant).
public class ExpressionLibrary {
	private const string CapturedGroupName = "Captured";

	public ActionUnitCatalog Catalog { get; }
	public IReadOnlySet<string> AffectedBones { get; private set; }

	private ActionUnitGroup? _capturedGroup;

	public ExpressionLibrary(ActionUnitCatalog catalog) {
		this.Catalog = catalog;
		this.AffectedBones = ComputeAffected(catalog);
	}

	private static HashSet<string> ComputeAffected(ActionUnitCatalog catalog)
		=> catalog.AllUnits().SelectMany(unit => unit.Bones.Keys).ToHashSet();

	public void AddCapturedUnit(ActionUnit unit) {
		var existing = this.Catalog.FindUnit(unit.Id);
		if (existing != null) {
			existing.Bones = unit.Bones;
			existing.Bidirectional = unit.Bidirectional;
			existing.UsePosition = unit.UsePosition;
		} else {
			if (this._capturedGroup == null) {
				this._capturedGroup = new ActionUnitGroup { Name = CapturedGroupName };
				this.Catalog.Groups.Add(this._capturedGroup);
			}
			this._capturedGroup.Units.Add(unit);
		}
		this.AffectedBones = ComputeAffected(this.Catalog);
	}

	public void RemoveUnit(string id) {
		foreach (var group in this.Catalog.Groups)
			group.Units.RemoveAll(unit => unit.Id == id);
		this.Catalog.Groups.RemoveAll(group => group.Units.Count == 0);
		if (this._capturedGroup != null && !this.Catalog.Groups.Contains(this._capturedGroup))
			this._capturedGroup = null;
		this.AffectedBones = ComputeAffected(this.Catalog);
	}
}
