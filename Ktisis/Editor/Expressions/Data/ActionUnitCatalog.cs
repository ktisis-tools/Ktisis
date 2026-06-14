using System.Collections.Generic;
using System.Linq;

namespace Ktisis.Editor.Expressions.Data;

public class ActionUnitGroup {
	public string Name { get; set; } = string.Empty;
	public List<ActionUnit> Units { get; set; } = new();
}

// The ordered catalog of Action Units. Order is significant: AU deltas are
// composed in catalog order (see ExpressionEditor.ApplyBlend), so the on-disk
// ordering is the authoritative blend order.
public class ActionUnitCatalog {
	public List<ActionUnitGroup> Groups { get; set; } = new();

	public IEnumerable<ActionUnit> AllUnits()
		=> this.Groups.SelectMany(group => group.Units);

	public ActionUnit? FindUnit(string id)
		=> this.AllUnits().FirstOrDefault(unit => unit.Id == id);
}
