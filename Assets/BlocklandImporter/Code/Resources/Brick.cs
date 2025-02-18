using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Blockland.Resources
{
    public class Brick : IResource
    {
        public enum BrickType
        {
            Brick,
            Mesh
        }
        public ResourceType Type => ResourceType.Brick;
        public BrickType brickGeometryType;
        public Vector3 colliderSize;
    }
}
