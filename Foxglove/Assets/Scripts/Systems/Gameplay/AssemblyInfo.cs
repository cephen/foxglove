using Foxglove.Core.State;
using Foxglove.Gameplay;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(State<GameState>))]
[assembly: RegisterGenericComponentType(typeof(NextState<GameState>))]
