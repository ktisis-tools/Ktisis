using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Plugin.Services;
using Dalamud.Utility;

using Lumina.Excel.GeneratedSheets2;

namespace Ktisis.Editor.Animation.Game;

public class GameAnimationData {
	private readonly List<GameAnimation> Animations = new();

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
	
	public async Task Build(
		IDataManager data
	) {
		await Task.Yield();
		this.FetchEmotes(data);
		this.FetchActions(data);
	}

	private void FetchEmotes(IDataManager data) {
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

	private void FetchActions(IDataManager data) {
		var actions = data.GetExcelSheet<Action>()!
			.Where(action => !action.Name.RawString.IsNullOrEmpty())
			.DistinctBy(action => (action.Name.RawString, action.Icon, action.AnimationStart.Row))
			.Select(action => new ActionAnimation(action));

		lock (this.Animations) {
			this.Animations.AddRange(actions);
		}
	}

	private async Task FetchTimelines(IDataManager data) {
		await Task.Yield();
	}
}
