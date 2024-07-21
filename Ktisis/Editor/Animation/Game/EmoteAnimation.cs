using Ktisis.Editor.Animation.Types;

using Lumina.Excel.GeneratedSheets2;

namespace Ktisis.Editor.Animation.Game;

public class EmoteAnimation(Emote emote, int index = 0) : GameAnimation {
	public override string Name => emote.Name.RawString;
	public override ushort Icon => emote.Icon;
	public override TimelineSlot Slot => (TimelineSlot)(emote.ActionTimeline[index].Value?.Stance ?? 0);
	
	public bool IsExpression => emote.EmoteCategory.Row == 3;

	public override void Apply(IAnimationEditor editor) {
		switch (index) {
			case 0:
				editor.PlayEmote(emote);
				break;
			default:
				editor.SetTimelineId((ushort)emote.ActionTimeline[index].Row);
				break;
		}
	}
}
