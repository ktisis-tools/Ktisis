using Ktisis.Editor.Animation.Types;

using Lumina.Excel.Sheets;

namespace Ktisis.Editor.Animation.Game;

public class TimelineAnimation(ActionTimeline timeline) : GameAnimation {
	public override string Name => timeline.Key.ExtractText();
	public override uint Icon => uint.MinValue;
	public override uint TimelineId => timeline.RowId;
	public override TimelineSlot Slot => (TimelineSlot)timeline.Stance;
}
