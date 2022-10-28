using System;
using System.Runtime.CompilerServices;

using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

using static Verse.AtomPhysics;
using static Verse.Chunk;
using static Verse.Atom;

namespace Verse
{
	[BurstCompile]
	public partial struct ProcessChunkParticlesJob : IJobEntity
	{
		[ReadOnly]
		public int tick;
		[ReadOnly]
		public ComponentLookup<Atom.Matter> matterOf;
		[ReadOnly]
		public ComponentLookup<Matter.AtomState> stateOf;
		[ReadOnly]
		public ComponentLookup<Matter.PhysicProperties> propsOf;

		[NativeDisableParallelForRestriction]
		public BufferLookup<AtomBufferElement> atomsOf;
		[NativeDisableParallelForRestriction]
		public ComponentLookup<DirtyArea> dirtyAreaOf;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Atom.Dynamics> dynamicsOf;

		/* these fields can be moved to locals */
		[NativeDisableParallelForRestriction]
		private DynamicBuffer<AtomBufferElement> atoms;

		public void Execute(Entity chunk, in Neighbourhood neighbours)
		{
			DirtyArea dirtyArea = dirtyAreaOf[chunk];
			if (!dirtyArea.active)
				return;
			dirtyArea.active = false;

			atoms = atomsOf[chunk];

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

			dirtyAreaOf[chunk] = dirtyArea;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ProcessAtom(
			ref DirtyArea dirtyArea,
			Neighbourhood neighbours,
			Entity atom, int x, int y
		)
		{
			Entity matter = matterOf[atom].value;
			Matter.State state = stateOf[matter].value;
			Matter.PhysicProperties physProps = propsOf[matter];

			Atom.Dynamics velocity = dynamicsOf[atom];

			switch (state)
			{
				case Matter.State.Solid:
					break;

				case Matter.State.Liquid:
					ProcessLiquid(ref dirtyArea, atoms, neighbours, new Coord(x, y), ref velocity, matter, physProps);
					break;

				case Matter.State.Gaseous:
					break;

				default:
					break;
			}

			dynamicsOf[atom] = velocity;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ProcessLiquid(
			ref DirtyArea dirtyArea, DynamicBuffer<AtomBufferElement> atoms, Neighbourhood neighbours,
			Coord initialCoord, ref Dynamics dyn, Entity matter, Matter.PhysicProperties props
		)
		{
			float motionLimit = motionPerTick;
			float2 displacement = float2.zero;

			bool moved = false;

			Coord lastCoord = initialCoord;
			Coord lastSwapCoord = initialCoord;
			DynamicBuffer<AtomBufferElement> lastSwapBuffer = atoms;

			while (motionLimit > 0f)
			{
				#region motion

				dyn.velocity += dyn.acceleration;
				float velMag = math.length(dyn.velocity);
				float delta = 1f;

				bool still = velMag == 0f;
				if (still)
				{
					motionLimit = 0;
				}
				else
				{
					delta /= velMag;
					motionLimit -= delta;
				}

				#endregion motion

				#region acceleration

				dyn.acceleration.y = perTickGravity * delta;

				#endregion acceleration

				if (still)
					break;

				float2 rollbackDisplacement = displacement;
				displacement = math.clamp(displacement + math.normalize(dyn.velocity), minDisplacement, maxDisplacement);

				Coord nextCoord = initialCoord + RoundToCoord(displacement);

				if (nextCoord == lastCoord)
				{
					continue;
				}

				float2 absDiff = math.abs(nextCoord - lastCoord);
				if (absDiff.x > 1f || absDiff.y > 1f)
					Debug.LogWarning("Pixel breach!!");

				if (!atoms.GetAtomNeighbourFallback(
					atomsOf, neighbours, nextCoord,
					out Entity nextAtom, out var swapBuffer, out Coord swapCoord
				))
				{
					continue;
				}

				if (nextAtom == Entity.Null)
				{
					swapBuffer.SetAtom(lastSwapCoord, lastSwapBuffer.GetAtom(lastSwapCoord));
					lastSwapBuffer.SetAtom(lastSwapCoord, Entity.Null);

					moved = true;
				}
				else
				{
					Entity nextMatter = matterOf[nextAtom].value;
					Matter.State nextState = stateOf[nextMatter].value;
					Matter.PhysicProperties nextProps = propsOf[nextMatter];

					Dynamics otherDyn;
					switch (nextState)
					{
						case Matter.State.Solid:
							displacement = rollbackDisplacement;
							continue;
						case Matter.State.Liquid:
							otherDyn = dynamicsOf[nextAtom];

							if (props.density < nextProps.density)
							{
								PerfectlyInelasticCollision(ref dyn.velocity, ref otherDyn.velocity);
								dynamicsOf[nextAtom] = otherDyn;
								break;
							}

							PassThrough(ref dyn.velocity, props, ref otherDyn.velocity, propsOf[nextMatter]);
							dynamicsOf[nextAtom] = otherDyn;

							AtomBufferExtention.Swap(lastSwapBuffer, lastSwapCoord, swapBuffer, swapCoord);
							moved = true;

							break;
						case Matter.State.Gaseous:
							otherDyn = dynamicsOf[nextAtom];
							PassThrough(ref dyn.velocity, props, ref otherDyn.velocity, propsOf[nextMatter]);
							dynamicsOf[nextAtom] = otherDyn;

							AtomBufferExtention.Swap(lastSwapBuffer, lastSwapCoord, swapBuffer, swapCoord);
							moved = true;

							break;
						default:
							displacement = rollbackDisplacement;
							continue;
					}
				}

				lastSwapBuffer = swapBuffer;
				lastSwapCoord = swapCoord;
				lastCoord = nextCoord;
			}

			if (moved)
			{
				CoordRect dirtyRect = CoordRect.CreateRectBetween(initialCoord, lastCoord, margin: 1);
				neighbours.MarkDirty(ref dirtyArea, dirtyAreaOf, dirtyRect, safe: true);
			}
		}
	}
}
