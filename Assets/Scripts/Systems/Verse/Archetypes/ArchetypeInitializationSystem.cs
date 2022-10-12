using Unity.Entities;

using static Verse.Chunk;
using static Verse.Atom;

namespace Verse
{
	[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
	[UpdateBefore(typeof(SpaceInitializationSystemGroup))]
	public partial class VerseInitializationSystem : SystemBase
	{
		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			Prefabs.RegisterCorePrefabs(
				region: GetSingleton<Prefabs.RegionPrefab>().prefab,
				chunk: GetSingleton<Prefabs.ChunkPrefab>().prefab
			);

			Archetypes.RegisterArchetypes(
				atom: EntityManager.CreateArchetype(
					ComponentType.ReadWrite<Atom.Matter>(),
					ComponentType.ReadWrite<Color>(),
					ComponentType.ReadWrite<Temperature>()
				)
			);

			Enabled = false;
		}

		protected override void OnUpdate()
		{
		}
	}
}