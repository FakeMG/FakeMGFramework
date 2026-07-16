using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Validates authored and evaluated values before they cross the type-erased runtime boundary.
    /// </summary>
    internal static class CycleOutputValueValidation
    {
        #region Public Methods

        public static bool TryValidate(
            IReadOnlyList<FloatCyclePoint> points,
            out string errorMessage)
        {
            for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
            {
                if (points[pointIndex] != null
                    && CycleNumericValidation.IsFinite(points[pointIndex].Value))
                {
                    continue;
                }

                errorMessage = $"Float point {pointIndex} contains a non-finite value.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public static bool TryValidate(
            IReadOnlyList<ColorCyclePoint> points,
            out string errorMessage)
        {
            for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
            {
                Color value = points[pointIndex] == null ? default : points[pointIndex].Value;
                if (points[pointIndex] != null
                    && IsFinite(value.r)
                    && IsFinite(value.g)
                    && IsFinite(value.b)
                    && IsFinite(value.a))
                {
                    continue;
                }

                errorMessage = $"Color point {pointIndex} contains a non-finite channel.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public static bool TryValidate(
            IReadOnlyList<RotationCyclePoint> points,
            out string errorMessage)
        {
            for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
            {
                Quaternion value = points[pointIndex] == null ? default : points[pointIndex].Value;
                if (points[pointIndex] != null
                    && IsFinite(value.x)
                    && IsFinite(value.y)
                    && IsFinite(value.z)
                    && IsFinite(value.w))
                {
                    continue;
                }

                errorMessage = $"Rotation point {pointIndex} contains a non-finite component.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public static bool IsValidRuntimeValue(object value, Type expectedType)
        {
            if (value == null || value.GetType() != expectedType)
            {
                return false;
            }

            if (value is float floatValue)
            {
                return IsFinite(floatValue);
            }

            if (value is Color colorValue)
            {
                return IsFinite(colorValue.r)
                       && IsFinite(colorValue.g)
                       && IsFinite(colorValue.b)
                       && IsFinite(colorValue.a);
            }

            if (value is Quaternion rotationValue)
            {
                return IsFinite(rotationValue.x)
                       && IsFinite(rotationValue.y)
                       && IsFinite(rotationValue.z)
                       && IsFinite(rotationValue.w);
            }

            return true;
        }

        public static object GetDefaultValue(Type valueType)
        {
            return valueType.IsValueType ? Activator.CreateInstance(valueType) : null;
        }

        #endregion

        #region Private Methods

        private static bool IsFinite(float value)
        {
            return CycleNumericValidation.IsFinite(value);
        }

        #endregion
    }
}
