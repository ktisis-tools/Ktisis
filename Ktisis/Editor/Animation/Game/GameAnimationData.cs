using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Plugin.Services;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Ktisis.Editor.Animation.Game;

public class GameAnimationData(IDataManager data) {
	private readonly List<GameAnimation> Animations = new();

	private ExcelSheet<ActionTimeline>? Timelines;

	public int Count {
		get {
			lock (this.Animations)
				return this.Animations.Count;
		}
	}
	
	public IEnumerable<GameAnimation> GetAll() {
		lock (this.Animations) {
			return this.Animations.AsReadOnly();
		}
	}
	
	public async Task Build() {
		await Task.Yield();
		this.FetchEmotes();
		this.FetchActions();
		this.FetchTimelines();
	}

	public ActionTimeline? GetTimelineById(uint id)
		=> this.Timelines?.GetRow(id);

	private void FetchEmotes() {
		var emotes = data.GetExcelSheet<Emote>()!
			.Where(emote => !emote.Name.IsEmpty)
			.SelectMany(MapEmotes)
			.DistinctBy(emote => (emote.Name, emote.Slot));

		lock (this.Animations) {
			this.Animations.AddRange(emotes);
		}

		IEnumerable<EmoteAnimation> MapEmotes(Emote emote) {
			for (var i = 0; i < emote.ActionTimeline.Count; i++) {
				var timeline = emote.ActionTimeline[i];
				if (timeline is { IsValid: true, RowId: not 0 })
					yield return new EmoteAnimation(emote, i);
			}
		}
	}

	private void FetchActions() {
		var actions = data.GetExcelSheet<Action>()!
			.Where(action => !action.Name.IsEmpty)
			.DistinctBy(action => (action.Name.ExtractText(), action.Icon, action.AnimationStart.RowId))
			.Select(action => new ActionAnimation(action));

		lock (this.Animations) {
			this.Animations.AddRange(actions);
		}
	}

	private void FetchTimelines() {
		this.Timelines ??= data.GetExcelSheet<ActionTimeline>()!;
		
		var timelines = this.Timelines
			.Where(timeline => !timeline.Key.IsEmpty)
			.Select(timeline => new TimelineAnimation(timeline));

		lock (this.Animations) {
			this.Animations.AddRange(timelines);
		}
	}
}
