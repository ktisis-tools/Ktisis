using Ktisis.Core.Attributes;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Components.Chara;

[Transient]
public class AnimationEditorTab {
	public AnimationEditorTab() { }
	
	public ActorEntity Target { set; private get; } = null!;
	
	public void Draw() {
		
	}
}
