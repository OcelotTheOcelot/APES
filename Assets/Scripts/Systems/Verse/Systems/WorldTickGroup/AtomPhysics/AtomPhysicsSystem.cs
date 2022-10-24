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
using Apes.Random;

namespace Verse
{
	[UpdateInGroup(typeof(VerseTickSystemGroup))]
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
				physicsQuery.SetSharedComponentFilter(new ProcessingBatchIndex { batchIndex = i });

				jobHandle = new ProcessChunkJob
				{
					tick = tick,
					matters = matters,
					states = states,
					physicProperties = physProps,
					atomBuffers = atomBuffers,
					dirtyAreas = dirtyAreas,
					velocities = velocities
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
			public ComponentLookup<DirtyArea> dirtyAreas;

			[NativeDisableParallelForRestriction]
			public ComponentLookup<Atom.Velocity> velocities;

			/* these fields can be moved to locals */
			[NativeDisableParallelForRestriction]
			private DynamicBuffer<AtomBufferElement> atoms;

			public void Execute(Entity chunk, in Neighbourhood neighbours)
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
				Entity atom, int x, int y
			)
			{
				Entity matter = matters[atom].value;
				Matter.State state = states[matter].value;
				Matter.PhysicProperties physProps = physicProperties[matter];

				Atom.Velocity velocity = velocities[atom];

				switch (state)
				{
					case Matter.State.Solid:
						break;

					case Matter.State.Liquid:
						ProcessLiquid(ref dirtyArea, atoms, neighbours, new Coord(x, y), ref velocity.value, matter, physProps);
						break;

					case Matter.State.Gaseous:
						break;

					default:
						break;
				}

				velocities[atom] = velocity;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void ProcessLiquid(
				ref DirtyArea dirtyArea, DynamicBuffer<AtomBufferElement> atoms, Neighbourhood neighbours,
				Coord initialCoord, ref float2 vel, Entity matter, Matter.PhysicProperties physProps
			)
			{
				float motion = motionPerTick;
				float2 displacement = float2.zero;

				bool moved = false;
				bool allEmpty = true;

				Coord lastCoord = initialCoord;
				Coord lastSwapCoord = initialCoord;
				DynamicBuffer<AtomBufferElement> lastSwapBuffer = atoms;

				while (motion > 0f)
				{
					#region gravity calculation

					if (atoms.GetAtomNeighbourFallback(atomBuffers, neighbours, lastCoord + Coord.south, out Entity nextAtom))
					{
						if (IsPassable(nextAtom, matter, physProps))
						{
							vel.y += perTickGravity;
						}
						else  // if other atom is liquid
						{
							// Perfectly inelastic collision
							float2 otherVel = velocities[nextAtom].value;
							vel.y = otherVel.y = (vel.y + otherVel.y) * .5f;
							velocities[nextAtom] = new Atom.Velocity(otherVel);
						}
					}

					#endregion gravity calculation


					float velMag = math.length(vel);
					if (velMag == 0f)
						break;

					motion -= 1f / velMag;


					#region displacement calculation

					Coord _prevDisp = initialCoord + RoundToCoord(displacement);
					displacement = math.clamp(displacement + math.normalize(vel), minDisplacement, maxDisplacement);

					Coord nextCoord = initialCoord + RoundToCoord(displacement);

					float2 absDiff = math.abs(nextCoord - lastCoord);

					if (absDiff.x > 1f || absDiff.y > 1f)
						Debug.LogWarning("Pixel breach!!");

					if (nextCoord == lastCoord)
					{
						continue;
					}

					if (!atoms.GetAtomNeighbourFallback(
						atomBuffers, neighbours, nextCoord,
						out nextAtom, out var swapBuffer, out Coord swapCoord
					))
					{
						lastCoord = nextCoord;
						continue;
					}

					if (IsPassable(nextAtom, matter, physProps))
					{
						moved = true;

						if (nextAtom != Entity.Null)
						{
							allEmpty = false;
							
							float2 otherVel = velocities[nextAtom].value;
							vel.y = otherVel.y = (vel.y + otherVel.y) * .5f;
							velocities[nextAtom] = new Atom.Velocity(otherVel);

							AtomBufferExtention.Swap(lastSwapBuffer, lastSwapCoord, swapBuffer, swapCoord);
						}
					}
					else
					{
                        lastCoord = nextCoord;
                        break;
					}

					lastSwapBuffer = swapBuffer;
					lastSwapCoord = swapCoord;
					lastCoord = nextCoord;

					#endregion displacement calculation
				}

				if (moved)
				{
					if (allEmpty)
						AtomBufferExtention.Swap(atoms, initialCoord, lastSwapBuffer, lastSwapCoord);

					CoordRect dirtyRect = CoordRect.CreateRectBetween(initialCoord, lastCoord, margin: 1);
					neighbours.MarkDirty(ref dirtyArea, dirtyAreas, dirtyRect, safe: true);
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private bool IsPassable(Entity otherAtom, Entity thisMatter, Matter.PhysicProperties thisProps)
			{
				if (otherAtom == Entity.Null)
					return true;

				Entity otherMatter = matters[otherAtom].value;
				if (thisMatter == otherMatter)
					return false;
				if (states[otherMatter].value == Matter.State.Solid)
					return false;

				Matter.PhysicProperties otherProps = physicProperties[otherMatter];
				return thisProps.density <= otherProps.density;
			}
		}
	}
}