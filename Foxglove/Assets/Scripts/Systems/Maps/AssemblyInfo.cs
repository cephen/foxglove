using System.Runtime.CompilerServices;
using Foxglove.Core.State;
using Foxglove.Maps;
using Unity.Entities;

[assembly: InternalsVisibleTo("Foxglove.Editor")]
[assembly: RegisterGenericComponentType(typeof(State<GeneratorState>))]
[assembly: RegisterGenericComponentType(typeof(NextState<GeneratorState>))]
