using System;
using System.Threading.Tasks;

using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Data.Files;
using Ktisis.Editor.Characters.State;
using Ktisis.GameData.Excel.Types;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Editor.Characters.Types;

public unsafe delegate void DisableDrawHandler(IGameObject gameObject, DrawObject* drawObject);

public interface ICharacterManager : IDisposable {
	public bool IsValid { get; }

	public event DisableDrawHandler? OnDisableDraw;
	
	public void Initialize();
	
	public ICustomizeEditor GetCustomizeEditor(ActorEntity actor);
	public IEquipmentEditor GetEquipmentEditor(ActorEntity actor);

	public bool TryGetStateForActor(IGameObject actor, out ActorEntity entity, out AppearanceState state);

	public void ApplyStateToGameObject(ActorEntity entity);

	public Task ApplyCharaFile(ActorEntity actor, CharaFile file, SaveModes modes = SaveModes.All, bool gameState = false);
	public Task<CharaFile> SaveCharaFile(ActorEntity actor);

	public Task ApplyNpc(ActorEntity actor, INpcBase npc, SaveModes modes = SaveModes.All, bool gameState = false);
}
