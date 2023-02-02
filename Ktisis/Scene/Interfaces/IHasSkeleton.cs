using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Structs.Actor;

namespace Ktisis.Scene.Interfaces {
	public interface IHasSkeleton : ITransformable {
		public unsafe abstract Skeleton* GetSkeleton();
		public unsafe abstract ActorModel* GetObject();
	}
}