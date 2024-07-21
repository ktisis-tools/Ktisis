using Ktisis.Editor.Animation.Types;

using Lumina.Excel.GeneratedSheets2;

namespace Ktisis.Editor.Animation.Game;

public class ActionAnimation(Action action) : GameAnimation {
	public override string Name => action.Name;
	public override ushort Icon => action.Icon;
	public override TimelineSlot Slot => (TimelineSlot)(action.AnimationEnd.Value?.Stance ?? 0);
	
	public override void Apply(IAnimationEditor editor) => editor.SetTimelineId((ushort)action.AnimationEnd.Row);
}
