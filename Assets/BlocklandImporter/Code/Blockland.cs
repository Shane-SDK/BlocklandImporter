using Blockland.Resources;
using UnityEngine;
using UnityEngine.UIElements;

namespace Blockland
{
    public static class Blockland
    {
        public static float studToUnityScaleFactor = 8.00f / 19.20f;
        public static float plateStudRatio = 3.2f / 8.0f;
        public const float metricToStudFactor = 2.0f;  // 0.5 => 1 stud apart
        public const float metricToPlateFactor = 5.0f;   // 0.2 => 1 plate apart
        public static Resources.Resources resources;
        static Blockland()
        {
            resources = new();
        }
        public static Vector3 BlocklandUnitsToStuds(Vector3 pos)
        {
            return new Vector3(
                        pos.x * Blockland.metricToStudFactor,
                        pos.y * Blockland.metricToPlateFactor,
                        pos.z * Blockland.metricToStudFactor);
        }
        public static Vector3 StudsToUnity(Vector3 studs)
        {
            studs *= studToUnityScaleFactor;
            studs.y *= plateStudRatio;
            return studs;
        }
    }
}
