﻿using Foxglove.Maps;
using Unity.Entities;
using UnityEngine.InputSystem;

namespace Foxglove.Editor.Maps {
    public partial struct RebuildMapHotkeySystem : ISystem {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state) {
            if (Keyboard.current.f5Key.wasPressedThisFrame) {
                SystemHandle mapGenSystemHandle = state.WorldUnmanaged.GetExistingUnmanagedSystem<MapGeneratorSystem>();
                state.EntityManager.SetComponentEnabled<GenerateMapRequest>(mapGenSystemHandle, true);
            }
        }
    }
}
