using System.Linq;
using Unity.Entities;

namespace Verse
{
	[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
	[UpdateBefore(typeof(SpaceInitializationSystemGroup))]
	public partial class VerseInitializationSystem : SystemBase
	{
		protected override void OnCreate()
		{
			base.OnCreate();

			RequireForUpdate<Prefabs.RegionPrefab>();
			RequireForUpdate<Prefabs.ChunkPrefab>();
		}

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			Prefabs.RegisterCorePrefabs(
				region: GetSingleton<Prefabs.RegionPrefab>().prefab,
				chunk: GetSingleton<Prefabs.ChunkPrefab>().prefab
			);

			ComponentType[] atomComponents = new[]
			{
				ComponentType.ReadWrite<Atom.Matter>(),
				ComponentType.ReadWrite<Atom.Color>(),
				ComponentType.ReadWrite<Atom.Temperature>(),
				ComponentType.ReadWrite<Atom.Velocity>()
            };
			ComponentType[] particleComponents = new[]
			{
				ComponentType.ReadWrite<Particle.OriginalAtom>(),
				ComponentType.ReadWrite<Particle.Velocity>(),
				ComponentType.ReadWrite<Particle.Position>()
			};

			Archetypes.RegisterArchetypes(
				atom: EntityManager.CreateArchetype(atomComponents),
				particle: EntityManager.CreateArchetype(particleComponents)
			);

			Enabled = false;
		}

		protected override void OnUpdate()
		{
		}
	}
}