namespace Foxglove.Input {
    /// <summary>
    /// Button-like inputs are tracked on a fixed tick basis,
    /// but systems that query input state sometimes run every frame.
    /// This systems allow a button press to last for multiple frames
    /// (or, in the event that rendering is slow, ensure that at least one frame receives button inputs)
    /// </summary>
    public struct FixedInputEvent {
        private bool _wasEverSet;
        private uint _lastSetTick;

        public void Set(uint tick) {
            _lastSetTick = tick;
            _wasEverSet = true;
        }

        public readonly bool IsSet(uint tick) => _wasEverSet && tick == _lastSetTick;
    }
}
