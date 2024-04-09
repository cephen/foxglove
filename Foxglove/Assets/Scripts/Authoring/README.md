# Foxglove.Authoring

> MonoBehaviour components for runtime conversion to entities

The first time a prefab with one of these authoring components is instantiated,
all authoring components on the prefab are converted into ECS components
and attached to a new entity with a Prefab tag.

More information on baking can be
found [here](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/baking-overview.html)
