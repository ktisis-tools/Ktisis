using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Plugin.Services;
using Dalamud.Utility;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets2;

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
			.Where(emote => !emote.Name.RawString.IsNullOrEmpty())
			.SelectMany(MapEmotes)
			.DistinctBy(emote => (emote.Name, emote.Slot));

		lock (this.Animations) {
			this.Animations.AddRange(emotes);
		}

		IEnumerable<EmoteAnimation> MapEmotes(Emote emote) {
			for (var i = 0; i < emote.ActionTimeline.Length; i++) {
				var timeline = emote.ActionTimeline[i];
				if (timeline is { Row: not 0 })
					yield return new EmoteAnimation(emote, i);
			}
		}
	}

	private void FetchActions() {
		var actions = data.GetExcelSheet<Action>()!
			.Where(action => !action.Name.RawString.IsNullOrEmpty())
			.DistinctBy(action => (action.Name.RawString, action.Icon, action.AnimationStart.Row))
			.Select(action => new ActionAnimation(action));

		lock (this.Animations) {
			this.Animations.AddRange(actions);
		}
	}

	private void FetchTimelines() {
		this.Timelines ??= data.GetExcelSheet<ActionTimeline>()!;
		
		var timelines = this.Timelines
			.Where(timeline => !timeline.Key.RawString.IsNullOrEmpty())
			.Select(timeline => new TimelineAnimation(timeline));

		lock (this.Animations) {
			this.Animations.AddRange(timelines);
		}
	}
}
