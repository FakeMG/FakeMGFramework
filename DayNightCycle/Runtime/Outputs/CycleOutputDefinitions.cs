using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Evaluates one configured output at a normalized time within a cycle.
    /// </summary>
    public interface ICycleOutputEvaluator
    {
        Type ValueType { get; }
        bool IsDiscrete { get; }
        object Evaluate(double presentationTimeSeconds);
    }

    /// <summary>
    /// Defines an extensible, profile-owned value evaluated from presentation time.
    /// </summary>
    [Serializable]
    public abstract class CycleOutputDefinition
    {
        [SerializeField] private string _outputId;
        [SerializeField] private float _profileChangeTransitionDurationSeconds = 1f;

        public CycleOutputId OutputId => new(_outputId);
        public float ProfileChangeTransitionDurationSeconds => _profileChangeTransitionDurationSeconds;
        public abstract Type ValueType { get; }
        public abstract bool IsDiscrete { get; }

        protected CycleOutputDefinition()
        {
        }

        protected CycleOutputDefinition(string outputId, float profileChangeTransitionDurationSeconds)
        {
            _outputId = outputId;
            _profileChangeTransitionDurationSeconds = profileChangeTransitionDurationSeconds;
        }

        #region Public Methods

        public abstract ICycleOutputEvaluator CreateEvaluator(
            double cycleDurationSeconds,
            IReadOnlyList<CyclePeriodDefinition> periods);

        public abstract object InterpolateProfileValue(object previousValue, object destinationValue, float progress01);

        public virtual bool TryValidate(
            double cycleDurationSeconds,
            ISet<CyclePeriodId> periodIds,
            out string errorMessage)
        {
            if (!OutputId.IsValid)
            {
                errorMessage = "The output identifier is empty.";
                return false;
            }

            if (_profileChangeTransitionDurationSeconds < 0f)
            {
                errorMessage = "Profile transition duration cannot be negative.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        #endregion
    }

    /// <summary>
    /// Stores one floating-point value at a cycle time.
    /// </summary>
    [Serializable]
    public sealed class FloatCyclePoint
    {
        [SerializeField] private double _timeSeconds;
        [SerializeField] private float _value;

        public double TimeSeconds => _timeSeconds;
        public float Value => _value;

        public FloatCyclePoint(double timeSeconds, float value)
        {
            _timeSeconds = timeSeconds;
            _value = value;
        }
    }

    /// <summary>
    /// Evaluates a cyclic floating-point curve.
    /// </summary>
    [Serializable]
    public sealed class FloatCycleOutputDefinition : CycleOutputDefinition
    {
        [SerializeField] private AnimationCurve _interpolationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private List<FloatCyclePoint> _points = new();

        public override Type ValueType => typeof(float);
        public override bool IsDiscrete => false;

        public FloatCycleOutputDefinition()
        {
        }

        public FloatCycleOutputDefinition(
            string outputId,
            float profileChangeTransitionDurationSeconds,
            AnimationCurve interpolationCurve,
            IEnumerable<FloatCyclePoint> points)
            : base(outputId, profileChangeTransitionDurationSeconds)
        {
            _interpolationCurve = interpolationCurve;
            _points = new List<FloatCyclePoint>(points);
        }

        #region Public Methods

        public override ICycleOutputEvaluator CreateEvaluator(
            double cycleDurationSeconds,
            IReadOnlyList<CyclePeriodDefinition> periods)
        {
            List<ContinuousCycleValue<float>> values = new(_points.Count);
            for (int pointIndex = 0; pointIndex < _points.Count; pointIndex++)
            {
                FloatCyclePoint point = _points[pointIndex];
                values.Add(new ContinuousCycleValue<float>(point.TimeSeconds, point.Value));
            }

            return new ContinuousCycleOutputEvaluator<float>(
                cycleDurationSeconds,
                values,
                _interpolationCurve,
                InterpolateFloat);
        }

        public override object InterpolateProfileValue(
            object previousValue,
            object destinationValue,
            float progress01)
        {
            float easedProgress01 = _interpolationCurve.Evaluate(progress01);
            return InterpolateFloat((float)previousValue, (float)destinationValue, easedProgress01);
        }

        public override bool TryValidate(
            double cycleDurationSeconds,
            ISet<CyclePeriodId> periodIds,
            out string errorMessage)
        {
            return base.TryValidate(cycleDurationSeconds, periodIds, out errorMessage)
                   && CycleOutputValidation.TryValidateContinuousPoints(
                       _points,
                       cycleDurationSeconds,
                       GetPointTime,
                       _interpolationCurve,
                       out errorMessage);
        }

        #endregion

        #region Private Methods

        private static float InterpolateFloat(float previousValue, float destinationValue, float progress01)
        {
            return Mathf.LerpUnclamped(previousValue, destinationValue, progress01);
        }

        private static double GetPointTime(FloatCyclePoint point)
        {
            return point.TimeSeconds;
        }

        #endregion
    }

    /// <summary>
    /// Stores one color value at a cycle time.
    /// </summary>
    [Serializable]
    public sealed class ColorCyclePoint
    {
        [SerializeField] private double _timeSeconds;
        [SerializeField] private Color _value = Color.white;

        public double TimeSeconds => _timeSeconds;
        public Color Value => _value;

        public ColorCyclePoint(double timeSeconds, Color value)
        {
            _timeSeconds = timeSeconds;
            _value = value;
        }
    }

    /// <summary>
    /// Evaluates a cyclic color curve.
    /// </summary>
    [Serializable]
    public sealed class ColorCycleOutputDefinition : CycleOutputDefinition
    {
        [SerializeField] private AnimationCurve _interpolationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private List<ColorCyclePoint> _points = new();

        public override Type ValueType => typeof(Color);
        public override bool IsDiscrete => false;

        public ColorCycleOutputDefinition()
        {
        }

        public ColorCycleOutputDefinition(
            string outputId,
            float profileChangeTransitionDurationSeconds,
            AnimationCurve interpolationCurve,
            IEnumerable<ColorCyclePoint> points)
            : base(outputId, profileChangeTransitionDurationSeconds)
        {
            _interpolationCurve = interpolationCurve;
            _points = new List<ColorCyclePoint>(points);
        }

        #region Public Methods

        public override ICycleOutputEvaluator CreateEvaluator(
            double cycleDurationSeconds,
            IReadOnlyList<CyclePeriodDefinition> periods)
        {
            List<ContinuousCycleValue<Color>> values = new(_points.Count);
            for (int pointIndex = 0; pointIndex < _points.Count; pointIndex++)
            {
                ColorCyclePoint point = _points[pointIndex];
                values.Add(new ContinuousCycleValue<Color>(point.TimeSeconds, point.Value));
            }

            return new ContinuousCycleOutputEvaluator<Color>(
                cycleDurationSeconds,
                values,
                _interpolationCurve,
                Color.LerpUnclamped);
        }

        public override object InterpolateProfileValue(
            object previousValue,
            object destinationValue,
            float progress01)
        {
            float easedProgress01 = _interpolationCurve.Evaluate(progress01);
            return Color.LerpUnclamped((Color)previousValue, (Color)destinationValue, easedProgress01);
        }

        public override bool TryValidate(
            double cycleDurationSeconds,
            ISet<CyclePeriodId> periodIds,
            out string errorMessage)
        {
            return base.TryValidate(cycleDurationSeconds, periodIds, out errorMessage)
                   && CycleOutputValidation.TryValidateContinuousPoints(
                       _points,
                       cycleDurationSeconds,
                       GetPointTime,
                       _interpolationCurve,
                       out errorMessage);
        }

        #endregion

        #region Private Methods

        private static double GetPointTime(ColorCyclePoint point)
        {
            return point.TimeSeconds;
        }

        #endregion
    }

    /// <summary>
    /// Stores one Euler-angle rotation at a cycle time for readable authoring.
    /// </summary>
    [Serializable]
    public sealed class RotationCyclePoint
    {
        [SerializeField] private double _timeSeconds;
        [SerializeField] private Vector3 _eulerDegrees;

        public double TimeSeconds => _timeSeconds;
        public Quaternion Rotation => Quaternion.Euler(_eulerDegrees);

        public RotationCyclePoint(double timeSeconds, Vector3 eulerDegrees)
        {
            _timeSeconds = timeSeconds;
            _eulerDegrees = eulerDegrees;
        }
    }

    /// <summary>
    /// Evaluates authored Euler points with quaternion interpolation.
    /// </summary>
    [Serializable]
    public sealed class RotationCycleOutputDefinition : CycleOutputDefinition
    {
        [SerializeField] private AnimationCurve _interpolationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private List<RotationCyclePoint> _points = new();

        public override Type ValueType => typeof(Quaternion);
        public override bool IsDiscrete => false;

        public RotationCycleOutputDefinition()
        {
        }

        public RotationCycleOutputDefinition(
            string outputId,
            float profileChangeTransitionDurationSeconds,
            AnimationCurve interpolationCurve,
            IEnumerable<RotationCyclePoint> points)
            : base(outputId, profileChangeTransitionDurationSeconds)
        {
            _interpolationCurve = interpolationCurve;
            _points = new List<RotationCyclePoint>(points);
        }

        #region Public Methods

        public override ICycleOutputEvaluator CreateEvaluator(
            double cycleDurationSeconds,
            IReadOnlyList<CyclePeriodDefinition> periods)
        {
            List<ContinuousCycleValue<Quaternion>> values = new(_points.Count);
            for (int pointIndex = 0; pointIndex < _points.Count; pointIndex++)
            {
                RotationCyclePoint point = _points[pointIndex];
                values.Add(new ContinuousCycleValue<Quaternion>(point.TimeSeconds, point.Rotation));
            }

            return new ContinuousCycleOutputEvaluator<Quaternion>(
                cycleDurationSeconds,
                values,
                _interpolationCurve,
                Quaternion.SlerpUnclamped);
        }

        public override object InterpolateProfileValue(
            object previousValue,
            object destinationValue,
            float progress01)
        {
            float easedProgress01 = _interpolationCurve.Evaluate(progress01);
            return Quaternion.SlerpUnclamped(
                (Quaternion)previousValue,
                (Quaternion)destinationValue,
                easedProgress01);
        }

        public override bool TryValidate(
            double cycleDurationSeconds,
            ISet<CyclePeriodId> periodIds,
            out string errorMessage)
        {
            return base.TryValidate(cycleDurationSeconds, periodIds, out errorMessage)
                   && CycleOutputValidation.TryValidateContinuousPoints(
                       _points,
                       cycleDurationSeconds,
                       GetPointTime,
                       _interpolationCurve,
                       out errorMessage);
        }

        #endregion

        #region Private Methods

        private static double GetPointTime(RotationCyclePoint point)
        {
            return point.TimeSeconds;
        }

        #endregion
    }

    /// <summary>
    /// Stores one boolean state change at an independent cycle time.
    /// </summary>
    [Serializable]
    public sealed class BoolCyclePoint
    {
        [SerializeField] private double _timeSeconds;
        [SerializeField] private bool _value;

        public double TimeSeconds => _timeSeconds;
        public bool Value => _value;

        public BoolCyclePoint(double timeSeconds, bool value)
        {
            _timeSeconds = timeSeconds;
            _value = value;
        }
    }

    /// <summary>
    /// Maps one named period start to a boolean state.
    /// </summary>
    [Serializable]
    public sealed class BoolPeriodValue
    {
        [SerializeField] private string _periodId;
        [SerializeField] private bool _value;

        public CyclePeriodId PeriodId => new(_periodId);
        public bool Value => _value;

        public BoolPeriodValue(string periodId, bool value)
        {
            _periodId = periodId;
            _value = value;
        }
    }

    /// <summary>
    /// Evaluates boolean changes from period conditions and independent points.
    /// </summary>
    [Serializable]
    public sealed class BoolCycleOutputDefinition : CycleOutputDefinition
    {
        [SerializeField] private List<BoolCyclePoint> _timelinePoints = new();
        [SerializeField] private List<BoolPeriodValue> _periodValues = new();

        public override Type ValueType => typeof(bool);
        public override bool IsDiscrete => true;

        public BoolCycleOutputDefinition()
        {
        }

        public BoolCycleOutputDefinition(
            string outputId,
            float profileChangeTransitionDurationSeconds,
            IEnumerable<BoolCyclePoint> timelinePoints,
            IEnumerable<BoolPeriodValue> periodValues)
            : base(outputId, profileChangeTransitionDurationSeconds)
        {
            _timelinePoints = new List<BoolCyclePoint>(timelinePoints);
            _periodValues = new List<BoolPeriodValue>(periodValues);
        }

        #region Public Methods

        public override ICycleOutputEvaluator CreateEvaluator(
            double cycleDurationSeconds,
            IReadOnlyList<CyclePeriodDefinition> periods)
        {
            List<DiscreteCycleChange<bool>> changes = new();
            AddPeriodChanges(periods, changes);
            for (int pointIndex = 0; pointIndex < _timelinePoints.Count; pointIndex++)
            {
                BoolCyclePoint point = _timelinePoints[pointIndex];
                changes.Add(new DiscreteCycleChange<bool>(point.TimeSeconds, 1, point.Value));
            }

            return new DiscreteCycleOutputEvaluator<bool>(changes);
        }

        public override object InterpolateProfileValue(
            object previousValue,
            object destinationValue,
            float progress01)
        {
            return progress01 >= 1f ? destinationValue : previousValue;
        }

        public override bool TryValidate(
            double cycleDurationSeconds,
            ISet<CyclePeriodId> periodIds,
            out string errorMessage)
        {
            return base.TryValidate(cycleDurationSeconds, periodIds, out errorMessage)
                   && CycleOutputValidation.TryValidateDiscretePoints(
                       _timelinePoints,
                       _periodValues,
                       cycleDurationSeconds,
                       periodIds,
                       GetPointTime,
                       GetPeriodId,
                       out errorMessage);
        }

        #endregion

        #region Private Methods

        private void AddPeriodChanges(
            IReadOnlyList<CyclePeriodDefinition> periods,
            ICollection<DiscreteCycleChange<bool>> changes)
        {
            for (int valueIndex = 0; valueIndex < _periodValues.Count; valueIndex++)
            {
                BoolPeriodValue periodValue = _periodValues[valueIndex];
                for (int periodIndex = 0; periodIndex < periods.Count; periodIndex++)
                {
                    CyclePeriodDefinition period = periods[periodIndex];
                    if (period.PeriodId.Equals(periodValue.PeriodId))
                    {
                        changes.Add(new DiscreteCycleChange<bool>(period.StartTimeSeconds, 0, periodValue.Value));
                        break;
                    }
                }
            }
        }

        private static double GetPointTime(BoolCyclePoint point)
        {
            return point.TimeSeconds;
        }

        private static CyclePeriodId GetPeriodId(BoolPeriodValue periodValue)
        {
            return periodValue.PeriodId;
        }

        #endregion
    }

    /// <summary>
    /// Stores one integer state change at an independent cycle time.
    /// </summary>
    [Serializable]
    public sealed class IntCyclePoint
    {
        [SerializeField] private double _timeSeconds;
        [SerializeField] private int _value;

        public double TimeSeconds => _timeSeconds;
        public int Value => _value;

        public IntCyclePoint(double timeSeconds, int value)
        {
            _timeSeconds = timeSeconds;
            _value = value;
        }
    }

    /// <summary>
    /// Maps one named period start to an integer or enum-backed state.
    /// </summary>
    [Serializable]
    public sealed class IntPeriodValue
    {
        [SerializeField] private string _periodId;
        [SerializeField] private int _value;

        public CyclePeriodId PeriodId => new(_periodId);
        public int Value => _value;

        public IntPeriodValue(string periodId, int value)
        {
            _periodId = periodId;
            _value = value;
        }
    }

    /// <summary>
    /// Evaluates integer changes from period conditions and independent points.
    /// </summary>
    [Serializable]
    public sealed class IntCycleOutputDefinition : CycleOutputDefinition
    {
        [SerializeField] private List<IntCyclePoint> _timelinePoints = new();
        [SerializeField] private List<IntPeriodValue> _periodValues = new();

        public override Type ValueType => typeof(int);
        public override bool IsDiscrete => true;

        public IntCycleOutputDefinition()
        {
        }

        public IntCycleOutputDefinition(
            string outputId,
            float profileChangeTransitionDurationSeconds,
            IEnumerable<IntCyclePoint> timelinePoints,
            IEnumerable<IntPeriodValue> periodValues)
            : base(outputId, profileChangeTransitionDurationSeconds)
        {
            _timelinePoints = new List<IntCyclePoint>(timelinePoints);
            _periodValues = new List<IntPeriodValue>(periodValues);
        }

        #region Public Methods

        public override ICycleOutputEvaluator CreateEvaluator(
            double cycleDurationSeconds,
            IReadOnlyList<CyclePeriodDefinition> periods)
        {
            List<DiscreteCycleChange<int>> changes = new();
            AddPeriodChanges(periods, changes);
            for (int pointIndex = 0; pointIndex < _timelinePoints.Count; pointIndex++)
            {
                IntCyclePoint point = _timelinePoints[pointIndex];
                changes.Add(new DiscreteCycleChange<int>(point.TimeSeconds, 1, point.Value));
            }

            return new DiscreteCycleOutputEvaluator<int>(changes);
        }

        public override object InterpolateProfileValue(
            object previousValue,
            object destinationValue,
            float progress01)
        {
            return progress01 >= 1f ? destinationValue : previousValue;
        }

        public override bool TryValidate(
            double cycleDurationSeconds,
            ISet<CyclePeriodId> periodIds,
            out string errorMessage)
        {
            return base.TryValidate(cycleDurationSeconds, periodIds, out errorMessage)
                   && CycleOutputValidation.TryValidateDiscretePoints(
                       _timelinePoints,
                       _periodValues,
                       cycleDurationSeconds,
                       periodIds,
                       GetPointTime,
                       GetPeriodId,
                       out errorMessage);
        }

        #endregion

        #region Private Methods

        private void AddPeriodChanges(
            IReadOnlyList<CyclePeriodDefinition> periods,
            ICollection<DiscreteCycleChange<int>> changes)
        {
            for (int valueIndex = 0; valueIndex < _periodValues.Count; valueIndex++)
            {
                IntPeriodValue periodValue = _periodValues[valueIndex];
                for (int periodIndex = 0; periodIndex < periods.Count; periodIndex++)
                {
                    CyclePeriodDefinition period = periods[periodIndex];
                    if (period.PeriodId.Equals(periodValue.PeriodId))
                    {
                        changes.Add(new DiscreteCycleChange<int>(period.StartTimeSeconds, 0, periodValue.Value));
                        break;
                    }
                }
            }
        }

        private static double GetPointTime(IntCyclePoint point)
        {
            return point.TimeSeconds;
        }

        private static CyclePeriodId GetPeriodId(IntPeriodValue periodValue)
        {
            return periodValue.PeriodId;
        }

        #endregion
    }

    /// <summary>
    /// Stores a typed continuous value at an evaluated time.
    /// </summary>
    internal readonly struct ContinuousCycleValue<T>
    {
        public double TimeSeconds { get; }
        public T Value { get; }

        public ContinuousCycleValue(double timeSeconds, T value)
        {
            TimeSeconds = timeSeconds;
            Value = value;
        }
    }

    /// <summary>
    /// Evaluates sorted continuous points including the cyclic final-to-first segment.
    /// </summary>
    internal sealed class ContinuousCycleOutputEvaluator<T> : ICycleOutputEvaluator
    {
        private readonly double _cycleDurationSeconds;
        private readonly List<ContinuousCycleValue<T>> _values;
        private readonly AnimationCurve _interpolationCurve;
        private readonly Func<T, T, float, T> _interpolate;

        public Type ValueType => typeof(T);
        public bool IsDiscrete => false;

        public ContinuousCycleOutputEvaluator(
            double cycleDurationSeconds,
            List<ContinuousCycleValue<T>> values,
            AnimationCurve interpolationCurve,
            Func<T, T, float, T> interpolate)
        {
            _cycleDurationSeconds = cycleDurationSeconds;
            _values = values;
            _values.Sort(CompareValuesByTime);
            _interpolationCurve = interpolationCurve;
            _interpolate = interpolate;
        }

        #region Public Methods

        public object Evaluate(double presentationTimeSeconds)
        {
            if (_values.Count == 1)
            {
                return _values[0].Value;
            }

            int destinationIndex = FindDestinationIndex(presentationTimeSeconds);
            int previousIndex = destinationIndex == 0 ? _values.Count - 1 : destinationIndex - 1;
            ContinuousCycleValue<T> previous = _values[previousIndex];
            ContinuousCycleValue<T> destination = _values[destinationIndex];

            double previousTimeSeconds = previous.TimeSeconds;
            double destinationTimeSeconds = destination.TimeSeconds;
            double evaluatedTimeSeconds = presentationTimeSeconds;
            if (destinationIndex == 0)
            {
                destinationTimeSeconds += _cycleDurationSeconds;
                if (evaluatedTimeSeconds < previousTimeSeconds)
                {
                    evaluatedTimeSeconds += _cycleDurationSeconds;
                }
            }

            double segmentDurationSeconds = destinationTimeSeconds - previousTimeSeconds;
            float segmentProgress01 = segmentDurationSeconds <= 0d
                ? 1f
                : (float)((evaluatedTimeSeconds - previousTimeSeconds) / segmentDurationSeconds);
            float easedProgress01 = _interpolationCurve.Evaluate(segmentProgress01);
            return _interpolate(previous.Value, destination.Value, easedProgress01);
        }

        #endregion

        #region Private Methods

        private int FindDestinationIndex(double presentationTimeSeconds)
        {
            for (int valueIndex = 0; valueIndex < _values.Count; valueIndex++)
            {
                if (_values[valueIndex].TimeSeconds > presentationTimeSeconds)
                {
                    return valueIndex;
                }
            }

            return 0;
        }

        private static int CompareValuesByTime(
            ContinuousCycleValue<T> left,
            ContinuousCycleValue<T> right)
        {
            return left.TimeSeconds.CompareTo(right.TimeSeconds);
        }

        #endregion
    }

    /// <summary>
    /// Stores one ordered discrete state change and its tie-breaking priority.
    /// </summary>
    internal readonly struct DiscreteCycleChange<T>
    {
        public double TimeSeconds { get; }
        public int Priority { get; }
        public T Value { get; }

        public DiscreteCycleChange(double timeSeconds, int priority, T value)
        {
            TimeSeconds = timeSeconds;
            Priority = priority;
            Value = value;
        }
    }

    /// <summary>
    /// Evaluates the most recent discrete state change in a cyclic stream.
    /// </summary>
    internal sealed class DiscreteCycleOutputEvaluator<T> : ICycleOutputEvaluator
    {
        private readonly List<DiscreteCycleChange<T>> _changes;

        public Type ValueType => typeof(T);
        public bool IsDiscrete => true;

        public DiscreteCycleOutputEvaluator(List<DiscreteCycleChange<T>> changes)
        {
            _changes = changes;
            _changes.Sort(CompareChanges);
        }

        #region Public Methods

        public object Evaluate(double presentationTimeSeconds)
        {
            DiscreteCycleChange<T> selectedChange = _changes[_changes.Count - 1];
            for (int changeIndex = 0; changeIndex < _changes.Count; changeIndex++)
            {
                DiscreteCycleChange<T> candidate = _changes[changeIndex];
                if (candidate.TimeSeconds > presentationTimeSeconds)
                {
                    break;
                }

                selectedChange = candidate;
            }

            return selectedChange.Value;
        }

        #endregion

        #region Private Methods

        private static int CompareChanges(
            DiscreteCycleChange<T> left,
            DiscreteCycleChange<T> right)
        {
            int timeComparison = left.TimeSeconds.CompareTo(right.TimeSeconds);
            return timeComparison != 0 ? timeComparison : left.Priority.CompareTo(right.Priority);
        }

        #endregion
    }

    /// <summary>
    /// Centralizes validation shared by the built-in output definition types.
    /// </summary>
    internal static class CycleOutputValidation
    {
        #region Public Methods

        public static bool TryValidateContinuousPoints<T>(
            IReadOnlyList<T> points,
            double cycleDurationSeconds,
            Func<T, double> getTimeSeconds,
            AnimationCurve interpolationCurve,
            out string errorMessage)
        {
            if (interpolationCurve == null)
            {
                errorMessage = "An interpolation curve is required.";
                return false;
            }

            if (points.Count == 0)
            {
                errorMessage = "At least one timeline point is required.";
                return false;
            }

            return TryValidateUniqueTimes(points, cycleDurationSeconds, getTimeSeconds, out errorMessage);
        }

        public static bool TryValidateDiscretePoints<TPoint, TPeriodValue>(
            IReadOnlyList<TPoint> timelinePoints,
            IReadOnlyList<TPeriodValue> periodValues,
            double cycleDurationSeconds,
            ISet<CyclePeriodId> periodIds,
            Func<TPoint, double> getTimeSeconds,
            Func<TPeriodValue, CyclePeriodId> getPeriodId,
            out string errorMessage)
        {
            if (timelinePoints.Count == 0 && periodValues.Count == 0)
            {
                errorMessage = "At least one timeline point or period value is required.";
                return false;
            }

            if (!TryValidateUniqueTimes(
                    timelinePoints,
                    cycleDurationSeconds,
                    getTimeSeconds,
                    out errorMessage))
            {
                return false;
            }

            HashSet<CyclePeriodId> configuredPeriodIds = new();
            for (int valueIndex = 0; valueIndex < periodValues.Count; valueIndex++)
            {
                CyclePeriodId periodId = getPeriodId(periodValues[valueIndex]);
                if (!periodIds.Contains(periodId))
                {
                    errorMessage = $"Period value references unknown period '{periodId}'.";
                    return false;
                }

                if (!configuredPeriodIds.Add(periodId))
                {
                    errorMessage = $"Period '{periodId}' has more than one value.";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        #endregion

        #region Private Methods

        private static bool TryValidateUniqueTimes<T>(
            IReadOnlyList<T> points,
            double cycleDurationSeconds,
            Func<T, double> getTimeSeconds,
            out string errorMessage)
        {
            HashSet<double> timesSeconds = new();
            for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
            {
                T point = points[pointIndex];
                if (point is null)
                {
                    errorMessage = $"Timeline point at index {pointIndex} is null.";
                    return false;
                }

                double timeSeconds = getTimeSeconds(point);
                if (timeSeconds < 0d || timeSeconds >= cycleDurationSeconds)
                {
                    errorMessage = $"Timeline point at index {pointIndex} is outside the cycle.";
                    return false;
                }

                if (!timesSeconds.Add(timeSeconds))
                {
                    errorMessage = $"More than one timeline point occurs at {timeSeconds} seconds.";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        #endregion
    }
}
