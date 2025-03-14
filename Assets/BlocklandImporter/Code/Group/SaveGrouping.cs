using Blockland.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Blockland.Group
{
    public class SaveGrouping : ScriptableObject
    {
        public SaveData save;
        public List<Group> groups;
        //public IEnumerable<int> GetBrickIndices(Group group)
        //{
        //    for (int i = 0; i < save.bricks.Count; i++)
        //    {
        //        BrickInstance brick = save.bricks[i];
        //        brick.GetTransformedBounds();
        //    }
        //}
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
