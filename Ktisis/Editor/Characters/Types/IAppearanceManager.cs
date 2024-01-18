using System;

using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Editor.Characters.Data;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Editor.Characters.Types;

public interface IAppearanceManager : IDisposable {
	public bool IsValid { get; }
	
	public ICustomizeEditor Customize { get; }
	public IEquipmentEditor Equipment { get; }
	
	public void Initialize();

	public bool TryGetStateForActor(GameObject actor, out ActorEntity entity, out AppearanceState state);
}
