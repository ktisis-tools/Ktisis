using Ktisis.Editor.Animation.Types;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Ktisis.Editor.Animation.Game;

public class EmoteAnimation(Emote emote, int index = 0) : GameAnimation {
	public override string Name => emote.Name.ExtractText();
	public override ushort Icon => emote.Icon;
	public override uint TimelineId => this.Timeline.RowId;
	public override TimelineSlot Slot => this.Timeline.IsValid ? (TimelineSlot)this.Timeline.Value.Stance : 0;

	public int Index => index;
	public uint EmoteId => emote.RowId;
	public bool IsExpression => emote.EmoteCategory.RowId == 3;
	
	private RowRef<ActionTimeline> Timeline => emote.ActionTimeline[index];
}
