using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.TextCore;

namespace TMPro
{
    internal static class MigrationUtils
    {
        public static int GetPointSize(this FaceInfo faceInfo)
        {
            var pointSize = faceInfo.pointSize;
            var pointSizeInt = Mathf.RoundToInt(pointSize);
            Assert.AreEqual(Mathf.Abs(pointSize - pointSizeInt), 0.0001f,
                $"Point size {pointSize} is not an integer. Rounded to {pointSizeInt}");
            return pointSizeInt;
        }
    }
}