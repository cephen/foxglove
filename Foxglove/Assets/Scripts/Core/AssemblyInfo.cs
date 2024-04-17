// This declaration enables the use of init only properties
// For example int Prop { get; init; }
// This allows a property to be set as readonly at construction time by an external script
// for more information see https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/init

namespace System.Runtime.CompilerServices {
    public struct IsExternalInit { }
}
