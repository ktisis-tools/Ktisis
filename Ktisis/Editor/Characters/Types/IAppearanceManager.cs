using System;

using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Editor.Characters.State;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Editor.Characters.Types;

public interface IAppearanceManager : IDisposable {
	public bool IsValid { get; }
	
	public void Initialize();

	public bool TryGetStateForActor(GameObject actor, out ActorEntity entity, out AppearanceState state);

	public ICustomizeEditor GetCustomizeEditor(ActorEntity actor);
	public IEquipmentEditor GetEquipmentEditor(ActorEntity actor);
}
