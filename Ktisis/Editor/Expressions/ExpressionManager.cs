using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Ktisis.Data.Expressions;
using Ktisis.Data.Serialization;
using Ktisis.Editor.Expressions.Handlers;
using Ktisis.Editor.Expressions.Types;

namespace Ktisis.Editor.Expressions;

public class ExpressionManager : IExpressionManager {
	private ExpressionsSchema _schema = null!;

	// Initialization

	public void Initialize() {
		this._schema = SchemaReader.ReadExpressions();
	}
	
	// Controllers

	private readonly List<IExpressionController> _controllers = [];

	public IExpressionController CreateController() {
		var controller = new ExpressionController(this);
		this._controllers.Add(controller);
		return controller;
	}

	public bool RemoveController(IExpressionController controller) {
		return this._controllers.Remove(controller);
	}

	public void ResetBlendStates() {
		foreach (var con in this._controllers)
			con.ResetBlendState();
	}
	
	// Data

	public bool TryGetSchemaFile(ushort raceSexId, [NotNullWhen(true)] out ExpressionsSchemaFile? entry) {
		return this._schema.TryGetEntry(raceSexId, out entry);
	}
	
	//private void 
}
