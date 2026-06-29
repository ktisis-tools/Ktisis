using System.Collections.Generic;
using System.Linq;

using Ktisis.Editor.Expressions.Data;

namespace Ktisis.Editor.Expressions;

// A loaded AU catalog for one race+gender+clan variant, plus its affected-bone set.
// Captured/removed AUs mutate this library (shared by all actors of that variant).
public class ExpressionLibrary {
	public ActionUnitCatalog Catalog { get; }
	public IReadOnlySet<string> AffectedBones { get; private set; }

	public ExpressionLibrary(ActionUnitCatalog catalog) {
		this.Catalog = catalog;
		this.AffectedBones = ComputeAffected(catalog);
	}

	private static HashSet<string> ComputeAffected(ActionUnitCatalog catalog)
		=> catalog.AllUnits().SelectMany(unit => unit.Bones.Keys).ToHashSet();
}
