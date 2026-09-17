using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Ktisis.Data.Expressions;

public record ExpressionsSchema {
	public readonly Dictionary<ushort, ExpressionsSchemaFile> Entries = [];

	public void AddEntry(ushort id, ExpressionsSchemaFile data)
		=> this.Entries.Add(id, data);

	public bool TryGetEntry(ushort id, [NotNullWhen(true)] out ExpressionsSchemaFile? entry)
		=> this.Entries.TryGetValue(id, out entry);
}
