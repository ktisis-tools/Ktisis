using System.Collections.Generic;

namespace Ktisis.Editor.Expressions.Data;

public class ActionUnitGroup {
    public string Name { get; init; } = string.Empty;
    public List<ActionUnit> Units { get; set; } = new(); //set seems unused, but, needed for deserialization
}
