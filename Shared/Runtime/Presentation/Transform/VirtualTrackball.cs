using UnityEngine;

namespace FakeMG.Framework.UI
{
    public static class VirtualTrackball
    {
        #region Public Methods

        public static Quaternion CalculateRotation(Vector2 previousPosition01, Vector2 currentPosition01)
        {
            return Quaternion.FromToRotation(ProjectToSphere(previousPosition01), ProjectToSphere(currentPosition01));
        }

        #endregion

        #region Private Methods

        private static Vector3 ProjectToSphere(Vector2 position01)
        {
            float squaredDistance01 = position01.sqrMagnitude;
            if (squaredDistance01 >= 1f)
            {
                Vector2 rimPosition01 = position01.normalized;
                return new Vector3(rimPosition01.x, rimPosition01.y, 0f);
            }

            return new Vector3(position01.x, position01.y, -Mathf.Sqrt(1f - squaredDistance01));
        }

        #endregion
    }
}
