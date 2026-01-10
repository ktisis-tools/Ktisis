using System;
using System.Collections.Generic;
using System.Linq;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Common.Utility;
using Ktisis.Core.Attributes;
using Ktisis.Structs.Objects;

namespace Ktisis.Services.Game;

[Singleton]
public class WorldService : IDisposable {
	private readonly GPoseService _gpose;
	private bool _init;

	public readonly List<WorldObject> Objects = [];
	public readonly Dictionary<nint, Transform> Transforms = new();

	public WorldService(
		GPoseService gpose
	) {
		this._gpose = gpose;
		this._gpose.StateChanged += this.OnGPoseEvent;
	}

	private void OnGPoseEvent(object sender, bool active) {
		this.Clean();
		if (active) this.BuildWorld();
	}

	public bool Touch(WorldObject obj) {
		if (!this.Objects.Contains(obj)) {
			Ktisis.Log.Error($"Object {obj} is not present in world!");
			return false;
		}
		if (this.Transforms.ContainsKey(obj.Address)) {
			Ktisis.Log.Error($"Object {obj} has already been added to scene!");
			return false;
		}

		this.Transforms.Add(obj.Address, obj.Transform);
		return true;
	}

	public void Refresh() {
		this.Clean();
		this.BuildWorld();
	}

	private void BuildWorld() {
		Ktisis.Log.Debug($"starting worldobject fetch...");
		this.Objects.AddRange(this.RecurseWorld().Where(obj => obj.ObjectType is ObjectType.BgObject));
		Ktisis.Log.Debug($"finished! {this.Objects.Count} bgobjects found");
		this._init = true;
	}

	private IEnumerable<WorldObject> RecurseWorld() {
		var worldObj = this.GetWorld();
		if (worldObj == null) yield break;
		yield return worldObj.Value;

		foreach (var sibling in worldObj.Value.GetSiblings()) {
			yield return sibling;
			foreach (var child in this.RecurseChildren(sibling))
				yield return child;
		}

		foreach (var child in this.RecurseChildren(worldObj.Value))
			yield return child;
	}

	private IEnumerable<WorldObject> RecurseChildren(WorldObject worldObj) {
		foreach (var child in worldObj.GetChildren()) {
			yield return child;
			foreach (var reChild in this.RecurseChildren(child))
				yield return reChild;
		}
	}

	private unsafe WorldObject? GetWorld() {
		var world = World.Instance();
		if (world == null) return null;
		return new WorldObject(&world->Object);
	}

	private void Clean() {
		if (!this._init) return;

		this.Objects.Clear();
		this.Transforms.Clear();
		this._init = false;
	}

	public void Dispose() {
		this.Clean();
		this._gpose.StateChanged -= this.OnGPoseEvent;
	}
}
