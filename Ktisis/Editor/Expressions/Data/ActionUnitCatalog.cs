using System.Collections.Generic;
using System.Linq;

namespace Ktisis.Editor.Expressions.Data;

public class ActionUnitCatalog {
	public List<ActionUnitGroup> Groups { get; set; } = new();

	public IEnumerable<ActionUnit> AllUnits()
		=> this.Groups.SelectMany(group => group.Units);

	public ActionUnit? FindUnit(string id)
		=> this.AllUnits().FirstOrDefault(unit => unit.Id == id);
}
