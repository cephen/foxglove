namespace Foxglove.Input {
    /// <summary>
    /// Systems running on worker threads may span multiple frames,
    /// this struct is used to work around frame rate dependency issues
    /// </summary>
    public struct FixedInputEvent {
        private bool _wasEverSet;
        private uint _lastSetTick;

        public void Set(uint tick) {
            _lastSetTick = tick;
            _wasEverSet = true;
        }

        public readonly bool IsSet(uint tick) => _wasEverSet && tick == _lastSetTick;


        public readonly uint TicksSinceLastSet(in uint tick) => tick - _lastSetTick;
    }
}
