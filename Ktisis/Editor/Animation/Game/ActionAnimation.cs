using Ktisis.Editor.Animation.Types;

using Lumina.Excel.GeneratedSheets2;

namespace Ktisis.Editor.Animation.Game;

public class ActionAnimation(Action action) : GameAnimation {
	public override string Name => action.Name;
	public override ushort Icon => action.Icon;
	public override uint TimelineId => action.AnimationEnd?.Row ?? 0;
	public override TimelineSlot Slot => (TimelineSlot)(action.AnimationEnd.Value?.Stance ?? 0);
}
