using System.Collections.Generic;

using Ktisis.Common.Utility;

namespace Ktisis.Data.Expressions;

public record ExpressionData {
	public string Id = string.Empty;
	public Dictionary<string, Transform> Transforms = [];
}
