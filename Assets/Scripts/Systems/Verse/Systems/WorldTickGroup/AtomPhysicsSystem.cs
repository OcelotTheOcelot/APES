using Unity.Entities;

using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

using Unity.Mathematics;
using System.Runtime.CompilerServices;
using UnityEngine;
using static Verse.Chunk;
using static Verse.AtomPhysics;
using System;
using Apes.UI;

namespace Verse
{
	[UpdateInGroup(typeof(WorldTickSystemGroup))]
	public partial class AtomPhysicsSystem : SystemBase
	{
		private EntityQuery physicsQuery;

		private readonly int processingBatches = 4;
		protected override void OnCreate()
		{
			base.OnCreate();

			physicsQuery = GetEntityQuery(
				ComponentType.ReadOnly<DirtyArea>(),
				ComponentType.ReadOnly<ProcessingBatchIndex>(),
				ComponentType.ReadOnly<Neighbourhood>()
			);
			physicsQuery.AddSharedComponentFilter(new ProcessingBatchIndex(-1));
		}

		protected override void OnUpdate()
		{
			int tick = TickerSystem.CurrentTick;

			var matters = GetComponentLookup<Atom.Matter>(isReadOnly: true);
			var states = GetComponentLookup<Matter.AtomState>(isReadOnly: true);
			var physProps = GetComponentLookup<Matter.PhysicProperties>(isReadOnly: true);
			var velocities = GetComponentLookup<Atom.Velocity>();
			var atomBuffers = GetBufferLookup<AtomBufferElement>();

			var dirtyAreas = GetComponentLookup<DirtyArea>();

			JobHandle jobHandle = default;
			for (int i = 0; i < processingBatches; i++)
			{
				physicsQuery.SetSharedComponentFilter(new Chunk.ProcessingBatchIndex { batchIndex = i });

				jobHandle = new ProcessChunkJob
				{
					tick = tick,
					matters = matters,
					states = states,
					physicProperties = physProps,
					velocities = velocities,
					atomBuffers = atomBuffers,
					dirtyAreas = dirtyAreas
				}.ScheduleParallel(physicsQuery, jobHandle);
				jobHandle.Complete();
			}
		}

		[BurstCompile]
		public partial struct ProcessChunkJob : IJobEntity
		{
			[ReadOnly]
			public int tick;
			[ReadOnly]
			public ComponentLookup<Atom.Matter> matters;
			[ReadOnly]
			public ComponentLookup<Matter.AtomState> states;
			[ReadOnly]
			public ComponentLookup<Matter.PhysicProperties> physicProperties;

			[NativeDisableParallelForRestriction]
			public BufferLookup<AtomBufferElement> atomBuffers;
			[NativeDisableParallelForRestriction]
			public ComponentLookup<Atom.Velocity> velocities;
			[NativeDisableParallelForRestriction]
			public ComponentLookup<DirtyArea> dirtyAreas;

			[NativeDisableParallelForRestriction]
			private DynamicBuffer<AtomBufferElement> atoms;

            public void Execute(
				Entity chunk,
				in Neighbourhood neighbours
			)
			{
				DirtyArea dirtyArea = dirtyAreas[chunk];
				if (!dirtyArea.active)
					return;
				dirtyArea.active = false;

				atoms = atomBuffers[chunk];

				Coord from = dirtyArea.From, to = dirtyArea.To;

				int oddity = (tick + from.y) & 0b1;

				int y = from.y;
				int height = to.y * Space.chunkSize;
				for (int rowShift = from.y * Space.chunkSize; rowShift <= height; rowShift += Space.chunkSize)
				{
					if (oddity == 1)
					{
						for (int x = from.x; x <= to.x; x++)
						{
							Entity atom = atoms[rowShift + x];
							if (atom != Entity.Null)
								ProcessAtom(ref dirtyArea, neighbours, atom, x, y);
						}
					}
					else
					{
						for (int x = to.x; x >= from.x; x--)
						{
							Entity atom = atoms[rowShift + x];
							if (atom != Entity.Null)
								ProcessAtom(ref dirtyArea, neighbours, atom, x, y);
						}
					}

					y++;
					oddity ^= 1;
				}

				dirtyAreas[chunk] = dirtyArea;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void ProcessAtom(
				ref DirtyArea dirtyArea,
				Neighbourhood neighbours,
				Entity atom,
				int x, int y
			)
			{
				Entity matter = matters[atom].value;
				Matter.State state = states[matter].value;
				Matter.PhysicProperties physProps = physicProperties[matter];
				float2 velocity = velocities[atom].value;

				switch (state)
				{
					case Matter.State.Solid:
						break;

					case Matter.State.Liquid:
						ProcessLiquid(ref dirtyArea, ref velocity, atoms, neighbours, matter, physProps, new Coord(x, y));
						break;

					case Matter.State.Gaseous:
						break;

					default:
						break;
				}

				velocities[atom] = velocity;
            }

			//[MethodImpl(MethodImplOptions.AggressiveInlining)]
			//private void ProcessLiquid(
			//	ref DirtyArea dirtyArea,
			//	ref float2 vel,
			//	DynamicBuffer<AtomBufferElement> atoms,
			//	Neighbourhood neighbours,
			//	Entity atomMatter,
			//	Matter.PhysicProperties atomProps,
			//	Coord atomCoord
			//)
			//{
   //             if (atoms.GetAtomNeighbourFallback(
			//			atomBuffers, neighbours, atomCoord + Coord.south,
			//			out Entity bottomAtom, out _, out _
			//			)
			//		)
			//	{
			//		if (IsPassable(bottomAtom, atomMatter, atomProps))
			//			vel.y = math.max(vel.y + perTickGravity, -maxVelocity);
			//		else
			//			vel.y = 0f;
   //             }

   //             bool moved = false;
			//	Coord lastLineCoord = atomCoord;

   //             int yDir = vel.y > 0 ? 1 : -1;
   //             int xDir = vel.x > 0 ? 1 : -1;

   //             Coord lastLineCoordSwappable = atomCoord;
   //             DynamicBuffer<AtomBufferElement> lastLineBuffer = atoms;

   //             float2 absVel = math.abs(vel);
			//	if (absVel.x > absVel.y)
			//	{
			//		float yShift = vel.y / absVel.x;
			//		int toX = Mathf.CeilToInt(absVel.x);
			//	}
			//	else
			//	{
			//		float xShift = vel.x / absVel.y;
			//		int toY = Mathf.CeilToInt(absVel.y);

			//		for (int deltaY = 1; deltaY <= toY; deltaY++)
			//		{
   //                     Coord nextCoord = atomCoord + new int2(Mathf.RoundToInt(xShift * deltaY), deltaY * yDir);

   //                     if (!atoms.GetAtomNeighbourFallback(
			//				atomBuffers, neighbours, nextCoord,
			//				out Entity otherAtom,
			//				out DynamicBuffer<AtomBufferElement> otherAtoms,
			//				out Coord otherCoord
			//				))
			//				break;

			//			if (!IsPassable(otherAtom, atomMatter, atomProps))
			//			{
   //                         if (atoms.GetAtomNeighbourFallback(
			//					atomBuffers, neighbours, new Coord(nextCoord.x - xDir, nextCoord.y),
			//					out Entity slopeAtom, out _, out _
   //                         ) && IsPassable(slopeAtom, atomMatter, atomProps))
			//				{
			//					vel = ReflectAgainst45(vel, -xDir, -yDir);
   //                             moved = true;
   //                         }
   //                         else
			//				{
   //                             vel.y = 0;
   //                         }

   //                         break;
			//			}

			//			moved = true;
   //                     lastLineCoord = nextCoord;
			//			lastLineCoordSwappable = otherCoord;
			//			lastLineBuffer = otherAtoms;
   //                 }
   //             }

   //             if (moved)
   //             {
   //                 AtomBufferExtention.Swap(atoms, atomCoord, lastLineBuffer, lastLineCoordSwappable);

   //                 CoordRect dirtyRect = CoordRect.CreateRectBetween(atomCoord, lastLineCoord, margin: 1);

   //                 dirtyArea.MarkDirty(dirtyRect, safe: true);
   //                 neighbours.MarkDirty(dirtyAreas, dirtyRect, safe: true);
   //             }
   //         }

            private bool IsPassable(Entity otherAtom, Entity thisMatter, Matter.PhysicProperties physProps)
            {
                if (otherAtom == Entity.Null)
                    return true;

                Entity otherMatter = matters[otherAtom].value;
                if (thisMatter == otherMatter)
                    return false;
                if (states[otherMatter].value == Matter.State.Solid)
                    return false;

                return physProps.density <= physicProperties[otherMatter].density;
            }
        }
	}
}