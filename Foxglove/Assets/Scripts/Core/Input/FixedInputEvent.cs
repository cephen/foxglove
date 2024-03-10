namespace Foxglove.Input {
    public struct FixedInputEvent {
        private byte _wasEverSet;
        private uint _lastSetTick;

        public void Set(uint tick) {
            _lastSetTick = tick;
            _wasEverSet = 1;
        }

        public readonly bool IsSet(uint tick) {
            if (_wasEverSet == 1) return tick == _lastSetTick;

            return false;
        }

        public readonly uint TicksSinceLastSet(in uint tick) => tick - _lastSetTick;
    }
}
