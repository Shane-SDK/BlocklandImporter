using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Blockland.Meshing.Occlusion
{
    public class Occlusion
    {
        // 0 - right        0+
        // 1 - left         0-
        // 2 - up           1+
        // 3 - down         1-
        // 4 - forward      2+
        // 5 - backward     2-

        OcclusionJob job;
        JobHandle handle;
        public void GetOcclusion(IList<BoundsInt> bricks, out byte[] occlusionFlags)
        {
            NativeArray<FaceData> faceDataTable = new(6, Allocator.TempJob);
            faceDataTable[0] = new FaceData(0, 1, 2, true);
            faceDataTable[1] = new FaceData(0, 1, 2, false);
            faceDataTable[2] = new FaceData(1, 0, 2, true);
            faceDataTable[3] = new FaceData(1, 0, 2, false);
            faceDataTable[4] = new FaceData(2, 1, 0, true);
            faceDataTable[5] = new FaceData(2, 1, 0, false);

            NativeArray<OcclusionBrick> jobBricks = new NativeArray<OcclusionBrick>(bricks.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<byte> occlusionData = new NativeArray<byte>(bricks.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < bricks.Count; i++)
            {
                jobBricks[i] = new OcclusionBrick(bricks[i]);
            }

            job = new OcclusionJob { bricks = jobBricks, occlusionFlags = occlusionData, faceDataTable = faceDataTable };
            handle = job.Schedule(bricks.Count, 128);

            while (true)
            {
                if (handle.IsCompleted)
                {
                    handle.Complete();
                    occlusionFlags = new byte[bricks.Count];
                    for (int i = 0; i < occlusionFlags.Length; i++)
                    {
                        occlusionFlags[i] = job.occlusionFlags[i];
                    }

                    break;
                }
            }

            handle.Complete();
            job.occlusionFlags.Dispose();
            job.bricks.Dispose();
            job.faceDataTable.Dispose();
        }
    }
    [BurstCompile(OptimizeFor = OptimizeFor.Performance, DisableSafetyChecks = true)]
    public struct OcclusionJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<FaceData> faceDataTable;
        [ReadOnly]
        public NativeArray<OcclusionBrick> bricks;
        [WriteOnly]
        public NativeArray<byte> occlusionFlags;
        public void Execute(int index)
        {
            OcclusionBrick brick = bricks[index];
            // iterate each side

            byte occlusionFlag = 0;

            for (int i = 0; i < 6; i++)
            {
                FaceData faceData = faceDataTable[i];
                int extrudedPosition;

                if (faceData.positiveDirection)
                {
                    extrudedPosition = brick.max[faceData.flattenAxisIndex];
                }
                else
                {
                    extrudedPosition = brick.min[faceData.flattenAxisIndex] - 1;
                }

                // get corresponding axis to move in
                // flatten i and j components
                uint rightLength = (uint)(brick.max[faceData.rightAxisIndex] - brick.min[faceData.rightAxisIndex]);
                uint upLength = (uint)(brick.max[faceData.upAxisIndex] - brick.min[faceData.upAxisIndex]);
                uint area = rightLength * upLength;
                bool doOcclude = true;
                int lastUsedNeighborIndex = -1;
                for (uint flattenedIndex = 0; flattenedIndex < area; flattenedIndex++)
                {
                    // check if space in volume does not have another bounds in it

                    uint up = flattenedIndex / rightLength;  // local positive offsets from min
                    uint right = flattenedIndex % rightLength;

                    int3 coordinate = default;
                    coordinate[faceData.rightAxisIndex] = brick.min[faceData.rightAxisIndex] + (int)right;
                    coordinate[faceData.upAxisIndex] = brick.min[faceData.upAxisIndex] + (int)up;
                    coordinate[faceData.flattenAxisIndex] = extrudedPosition;

                    bool foundBrick = false;

                    if (lastUsedNeighborIndex != -1 && bricks[lastUsedNeighborIndex].Contains(coordinate))  // early escape
                    {
                        foundBrick = true;
                    }

                    if (!foundBrick)
                    {
                        for (int n = 0; n < bricks.Length; n++)
                        {
                            if (n == index) continue;

                            OcclusionBrick neighbor = bricks[n];
                            if (neighbor.Contains(coordinate))
                            {
                                lastUsedNeighborIndex = n;
                                foundBrick = true;
                                break;
                            }
                        }
                    }
                    
                    if (!foundBrick)
                    {
                        doOcclude = false;
                        break;
                    }
                }

                if (doOcclude)
                    occlusionFlag = (byte)(occlusionFlag | (1 << i));
            }

            occlusionFlags[index] = occlusionFlag;
        }
    }
    public struct OcclusionBrick
    {
        public OcclusionBrick(BoundsInt bounds)
        {
            min = new int3(bounds.min.x, bounds.min.y, bounds.min.z);
            max = new int3(bounds.max.x, bounds.max.y, bounds.max.z);
        }
        public int3 min;
        public int3 max;
        public bool Contains(int3 point)
        {
            if (point.x < min.x) return false;
            if (point.y < min.y) return false;
            if (point.z < min.z) return false;
            if (point.x >= max.x) return false;
            if (point.y >= max.y) return false;
            if (point.z >= max.z) return false;

            return true;
        }
    }
    public struct FaceData
    {
        public FaceData(int axis, int rightAxisIndex, int upAxisIndex, bool positiveDirection)
        {
            flattenAxisIndex = axis;
            this.rightAxisIndex = rightAxisIndex;
            this.upAxisIndex = upAxisIndex;
            this.positiveDirection = positiveDirection;
        }
        public int flattenAxisIndex;
        public int rightAxisIndex;
        public int upAxisIndex;
        public bool positiveDirection;
    }
}
