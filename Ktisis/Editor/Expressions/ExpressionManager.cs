using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.ClientState.Objects.Enums;

using Ktisis.Data.Serialization;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Expressions.Data;
using Ktisis.Editor.Expressions.Handlers;
using Ktisis.Editor.Expressions.Types;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Editor.Expressions;

public class ExpressionManager(IEditorContext ctx) : IExpressionManager {
	// Used when the actor's race/gender/clan has no embedded catalog.
	private const string FallbackKey = "Hyur_Feminine_Midlander";

	private readonly Dictionary<string, ExpressionLibrary> _libraries = new();

	public void Initialize() {
		ctx.Posing.OnPosingChanged += this.HandlePosingChanged;
	}

	public IExpressionEditor GetEditor(ActorEntity actor) => new ExpressionEditor(this, ctx, actor);

	private void HandlePosingChanged(bool isEnabled) {
		if (!isEnabled)
			this.OnPosingDisabled();
	}

	private void OnPosingDisabled() {
		foreach (var actor in ctx.Scene.Recurse().OfType<ActorEntity>()) {
			actor.Pose?.ExpressionState.Reset();
		}
	}

	public ExpressionLibrary GetLibrary(ActorEntity actor) {
		var key = ResolveKey(actor);

		if (this._libraries.TryGetValue(key, out var library))
			return library;

		var catalog = SchemaReader.ReadActionUnits(key)
			?? SchemaReader.ReadActionUnits(FallbackKey)
			?? new ActionUnitCatalog();

		library = new (catalog);
		this._libraries[key] = library;

		return library;
	}

	private static string ResolveKey(ActorEntity actor) {
		var race = actor.GetCustomizeValue(CustomizeIndex.Race);
		var gender = actor.GetCustomizeValue(CustomizeIndex.Gender);
		var tribe = actor.GetCustomizeValue(CustomizeIndex.Tribe);

		var genderName = gender == 1 ? "Feminine" : "Masculine";

		return (race, tribe) switch {
			(1, 1) => $"Hyur_{genderName}_Midlander",
			(1, 2) => $"Hyur_{genderName}_Highlander",
			(2, _) => $"Elezen_{genderName}",
			(3, _) => $"Lalafell_{genderName}",
			(5, 9) => $"Roegadyn_{genderName}_SeaWolf",
			(5, 10) => $"Roegadyn_{genderName}_Hellsguard",
			(4, _) => $"Miqote_{genderName}",
			(6, _) => $"AuRa_{genderName}",
			(7, _) => $"Hrothgar_{genderName}",
			(8, _) => $"Viera_{genderName}",
			_ => FallbackKey,
		};
	}
}
