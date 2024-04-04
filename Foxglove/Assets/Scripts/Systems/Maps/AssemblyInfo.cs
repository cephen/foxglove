using System.Runtime.CompilerServices;
using Foxglove.Maps;
using Foxglove.State;
using Unity.Entities;

[assembly: InternalsVisibleTo("Foxglove.State")]
[assembly: RegisterGenericComponentType(typeof(State<GeneratorState>))]
[assembly: RegisterGenericComponentType(typeof(NextState<GeneratorState>))]
