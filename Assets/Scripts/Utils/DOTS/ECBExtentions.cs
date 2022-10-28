using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Entities;
using UnityEngine;

public static class ECBExtentions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static DynamicBuffer<T> CloneBuffer<T>(this EntityCommandBuffer ecb, Entity entity, DynamicBuffer<T> original) where T : unmanaged, IBufferElementData
	{
		DynamicBuffer<T> newBuffer = ecb.SetBuffer<T>(entity);
		foreach (T value in original)
			newBuffer.Add(value);
		return newBuffer;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static DynamicBuffer<T> CloneBuffer<T>(this EntityCommandBuffer.ParallelWriter ecb, int sortKey, Entity entity, DynamicBuffer<T> original) where T : unmanaged, IBufferElementData
	{
		DynamicBuffer<T> newBuffer = ecb.SetBuffer<T>(sortKey, entity);
		foreach (T value in original)
			newBuffer.Add(value);
		return newBuffer;
	}
}
