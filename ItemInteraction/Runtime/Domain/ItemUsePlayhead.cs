namespace FakeMG.ItemInteraction
{
    public sealed class ItemUsePlayhead
    {
        private float _playbackSpeedMultiplier;
        private float _sequenceTimeSeconds;
        private bool _stopsAtSequenceEnd;

        public ItemAnimationProfileSO AnimationProfileSO { get; private set; }
        public int ClipIndex { get; private set; }
        public bool IsPlaying => AnimationProfileSO != null;
        public bool HasFinished => _stopsAtSequenceEnd && _sequenceTimeSeconds >= AnimationProfileSO.UseDurationSeconds;
        public ItemAnimationPose CurrentPose => AnimationProfileSO.GetUsePose(ClipIndex, _playbackSpeedMultiplier);

        #region Public Methods

        /// <param name="stopsAtSequenceEnd">False keeps the last clip on until <see cref="Stop"/>, for uses whose owner ends them.</param>
        public void Start(ItemAnimationProfileSO animationProfileSO, float playbackSpeedMultiplier, bool stopsAtSequenceEnd)
        {
            AnimationProfileSO = animationProfileSO;
            _playbackSpeedMultiplier = playbackSpeedMultiplier;
            _stopsAtSequenceEnd = stopsAtSequenceEnd;
            _sequenceTimeSeconds = 0f;
            ClipIndex = 0;
        }

        /// <returns>Whether the sequence moved on to its next clip, which must then start from its first frame.</returns>
        public bool Advance(float deltaSeconds)
        {
            _sequenceTimeSeconds += deltaSeconds * _playbackSpeedMultiplier;
            int clipIndex = AnimationProfileSO.GetUseClipIndex(_sequenceTimeSeconds);
            bool hasReachedNextClip = clipIndex != ClipIndex;
            ClipIndex = clipIndex;
            return hasReachedNextClip;
        }

        public void Stop()
        {
            AnimationProfileSO = null;
        }

        #endregion
    }
}
