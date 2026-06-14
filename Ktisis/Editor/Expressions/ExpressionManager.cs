using System.Collections.Generic;

using Dalamud.Game.ClientState.Objects.Enums;

using Ktisis.Data.Serialization;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Expressions.Data;
using Ktisis.Editor.Expressions.Handlers;
using Ktisis.Editor.Expressions.Types;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Editor.Expressions;

public class ExpressionManager : IExpressionManager {
	// Used when the actor's race/gender/clan has no embedded catalog.
	private const string FallbackKey = "Hyur_Feminine_Midlander";

	private readonly IEditorContext _ctx;

	private readonly Dictionary<ushort, ExpressionState> _states = new();
	private readonly Dictionary<string, ExpressionLibrary> _libraries = new();

	public ExpressionManager(IEditorContext ctx) {
		this._ctx = ctx;
	}

	public void Initialize() { }

	public IExpressionEditor GetEditor(ActorEntity actor) => new ExpressionEditor(this, this._ctx, actor);

	public ExpressionState GetState(ushort objectIndex) {
		if (!this._states.TryGetValue(objectIndex, out var state)) {
			state = new ExpressionState();
			this._states[objectIndex] = state;
		}
		return state;
	}

	// Resolves (and caches) the AU catalog matching the actor's race/gender/clan.
	public ExpressionLibrary GetLibrary(ActorEntity actor) {
		var key = ResolveKey(actor);
		if (!this._libraries.TryGetValue(key, out var library)) {
			var catalog = SchemaReader.ReadActionUnits(key)
				?? SchemaReader.ReadActionUnits(FallbackKey)
				?? new ActionUnitCatalog();
			library = new ExpressionLibrary(catalog);
			this._libraries[key] = library;
		}
		return library;
	}

	private static string ResolveKey(ActorEntity actor) {
		var race = actor.GetCustomizeValue(CustomizeIndex.Race);
		var gender = actor.GetCustomizeValue(CustomizeIndex.Gender);
		var tribe = actor.GetCustomizeValue(CustomizeIndex.Tribe);

		if (!RaceFolder.TryGetValue(race, out var raceName))
			return FallbackKey;

		var genderName = gender == 1 ? "Feminine" : "Masculine";

		if (ClanRaces.Contains(race) && ClanFolder.TryGetValue(tribe, out var clan))
			return $"{raceName}_{genderName}_{clan}";

		return $"{raceName}_{genderName}";
	}

	// FFXIV customize Race byte -> Anamnesis race folder.
	private static readonly Dictionary<byte, string> RaceFolder = new() {
		{ 1, "Hyur" }, { 2, "Elezen" }, { 3, "Lalafell" }, { 4, "Miqote" },
		{ 5, "Roegadyn" }, { 6, "AuRa" }, { 7, "Hrothgar" }, { 8, "Viera" }
	};

	// Races whose library splits by clan (others have no clan subfolder).
	private static readonly HashSet<byte> ClanRaces = new() { 1, 2, 3, 4, 5 };

	// FFXIV customize Tribe byte -> clan folder (only for ClanRaces).
	private static readonly Dictionary<byte, string> ClanFolder = new() {
		{ 1, "Midlander" }, { 2, "Highlander" }, { 3, "Wildwood" }, { 4, "Duskwight" },
		{ 5, "Plainsfolk" }, { 6, "Dunesfolk" }, { 7, "SeekerOfTheSun" }, { 8, "KeeperOfTheMoon" },
		{ 9, "SeaWolf" }, { 10, "Hellsguard" }
	};
}
