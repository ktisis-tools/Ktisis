using Ktisis.Scene.Entities.Game;
using Ktisis.Structs.Actors;

namespace Ktisis.Editor.Animation.Types;

public interface IAnimationManager {
	public void Initialize();

	public IAnimationEditor GetAnimationEditor(ActorEntity actor);

	public void SetPose(ActorEntity actor, PoseModeEnum poseMode, byte pose = 0xFF);
	
	public bool PlayEmote(ActorEntity actor, uint id);
	public bool PlayTimeline(ActorEntity actor, uint id);
}
