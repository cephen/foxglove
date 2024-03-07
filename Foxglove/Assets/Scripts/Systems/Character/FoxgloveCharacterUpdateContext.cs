using Unity.Entities;

namespace Foxglove.Character {
    /// <summary>
    /// This class manages global state for character updates.
    /// </summary>
    public struct FoxgloveCharacterUpdateContext {
        // TODO: Implement AI Blackboard using ComponentLookups & NativeCollections here
        // public ComponentLookup<FoxgloveCharacterSettings> CharacterSettingsLookup;

        // Initialize global state
        public void OnSystemCreate(ref SystemState state) {
            // CharacterSettingsLookup = state.GetComponentLookup<FoxgloveCharacterSettings>(isReadOnly: true);
        }

        // Update global state
        public void OnSystemUpdate(ref SystemState state) {
            // CharacterSettingsLookup.Update(ref state);
        }
    }
}
