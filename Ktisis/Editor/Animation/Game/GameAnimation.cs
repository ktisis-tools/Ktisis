using Ktisis.Editor.Animation.Types;

namespace Ktisis.Editor.Animation.Game;

public abstract class GameAnimation {
	public abstract string Name { get; }
	public abstract ushort Icon { get; }
	public abstract uint TimelineId { get; }
	public abstract TimelineSlot Slot { get; }
}
