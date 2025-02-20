using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blockland.Objects
{
    public class BrickData : ScriptableObject
    {
        public Vector3Int size;
        public BrickType type;

        public static BrickData CreateFromReader(Reader reader)
        {
            BrickData data = ScriptableObject.CreateInstance<BrickData>();
            reader.ReadLine();
            data.size = new Vector3Int(reader.ParseLineInt(0), reader.ParseLineInt(2), reader.ParseLineInt(1));
            data.type = reader.ReadLine().ToLower() == "brick" ? BrickType.Brick : BrickType.Special;

            return data;
        }
    }
    public enum BrickType
    {
        Brick,
        Special
    }
}
