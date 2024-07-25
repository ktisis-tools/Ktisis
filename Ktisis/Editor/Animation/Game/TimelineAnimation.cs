using Ktisis.Editor.Animation.Types;

using Lumina.Excel.GeneratedSheets2;

namespace Ktisis.Editor.Animation.Game;

public class TimelineAnimation(ActionTimeline timeline) : GameAnimation {
	public override string Name => timeline.Key.RawString;
	public override ushort Icon => ushort.MinValue;
	public override uint TimelineId => timeline.RowId;
	public override TimelineSlot Slot => (TimelineSlot)timeline.Stance;
}
