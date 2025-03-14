using Blockland.Objects;
using Octree;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blockland.Group
{
    public class SaveGrouping : ScriptableObject
    {
        public SaveData save;
        public List<Group> groups;
        public IEnumerable<int> GetBrickIndices(Group group)
        {
            for (int i = 0; i < save.bricks.Count; i++)
            {
                BrickInstance brick = save.bricks[i];
                brick.GetTransformedBounds(out BoundsInt intBounds);

                Bounds bounds = new Bounds(intBounds.center, intBounds.size);
                if (group.Contains(in bounds))
                    yield return i;
            }
        }
        public IEnumerable<int> GetBrickIndices(Group group, BoundsOctree<int> octree)
        {
            foreach (VolumeSelection volume in group.volumes)
            {
                BoundingBox box = new BoundingBox(Extensions.Vec3(volume.bounds.center), Extensions.Vec3(volume.bounds.size));
                List<int> colliding = octree.GetColliding(box);
                
                if (volume.selectionMode == SelectionMode.Overlap)
                {
                    foreach (int index in colliding)
                        yield return index;
                }
                else  // Contain
                {
                    foreach (int index in colliding)  // ensure indices are entirely contained
                    {
                        save.bricks[index].GetTransformedBounds(out Bounds brickBounds);

                        if (Group.Contains(in volume.bounds, in brickBounds))
                            yield return index;
                    }
                }
            }
        }
        public static SaveGrouping CreateFromSave(SaveData save)
        {
            SaveGrouping group = CreateInstance<SaveGrouping>();
            group.save = save;
            group.name = save.name;

            return group;
        }
    }

    [Serializable]
    public class Group
    {
        public string name;
        public List<VolumeSelection> volumes;

        public bool Contains(in Bounds bounds)
        {
            foreach (VolumeSelection volume in volumes)
            {
                if (volume.selectionMode == SelectionMode.Overlap)
                {
                    if (Overlap(in volume.bounds, in bounds))
                        return true;
                }
                else  // Contain
                {
                    if (Contains(in volume.bounds, in bounds))
                        return true;
                }
            }

            return false;
        }

        public static bool Overlap(in Bounds a, in Bounds b)
        {
            for (int i = 0; i < 3; i++)
            {
                if (a.min[i] > b.max[i]) return false;
                if (a.max[i] < b.min[i]) return false;
            }

            return true;
        }
        public static bool Contains(in Bounds outer, in Bounds inner)
        {
            for (int i = 0; i < 3; i++)
            {
                if (inner.min[i] < outer.min[i]) return false;
                if (inner.max[i] > outer.max[i]) return false;
            }

            return true;
        }
    }

    [Serializable]
    public struct VolumeSelection
    {
        public Bounds bounds;
        public SelectionMode selectionMode;
    }

    public enum SelectionMode
    {
        Overlap,    // bricks only have to overlap with bounds
        Contain,    // bricks have to be entirely contained within bounds
    }
}
