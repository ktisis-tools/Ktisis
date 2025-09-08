using Ktisis.Editor.Animation.Types;

using Lumina.Excel.Sheets;

namespace Ktisis.Editor.Animation.Game;

public class ActionAnimation(Action action) : GameAnimation {
	public override string Name => action.Name.ExtractText();
	public override ushort Icon => action.Icon;
	public override uint TimelineId => action.AnimationEnd.IsValid ? action.AnimationEnd.RowId : 0;
	public override TimelineSlot Slot => (TimelineSlot)(action.AnimationEnd.IsValid ? action.AnimationEnd.Value.Stance : 0);
}
