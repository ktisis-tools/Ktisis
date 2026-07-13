using Ktisis.Scene.Entities.Game;

namespace Ktisis.Editor.Expressions.Types;

public interface IExpressionManager {
	void Initialize();

	IExpressionEditor GetEditor(ActorEntity actor);

	// The AU catalog (and affected-bone set) matching the actor's race/gender/clan.
	ExpressionLibrary GetLibrary(ActorEntity actor);
}
