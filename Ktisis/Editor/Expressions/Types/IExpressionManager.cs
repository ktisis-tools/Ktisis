using System.Diagnostics.CodeAnalysis;

using Ktisis.Data.Expressions;

namespace Ktisis.Editor.Expressions.Types;

public interface IExpressionManager {
	public void Initialize();

	public IExpressionController CreateController();
	public bool RemoveController(IExpressionController controller);

	public bool TryGetSchemaFile(ushort raceSexId, [NotNullWhen(true)] out ExpressionsSchemaFile? entry);
}
