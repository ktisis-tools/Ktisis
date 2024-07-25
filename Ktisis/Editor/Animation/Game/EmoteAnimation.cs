using Ktisis.Editor.Animation.Types;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets2;

namespace Ktisis.Editor.Animation.Game;

public class EmoteAnimation(Emote emote, int index = 0) : GameAnimation {
	public override string Name => emote.Name.RawString;
	public override ushort Icon => emote.Icon;
	public override uint TimelineId => this.Timeline.Row;
	public override TimelineSlot Slot => (TimelineSlot)(this.Timeline.Value?.Stance ?? 0);

	public int Index => index;
	public uint EmoteId => emote.RowId;
	public bool IsExpression => emote.EmoteCategory.Row == 3;
	
	private LazyRow<ActionTimeline> Timeline => emote.ActionTimeline[index];
}
