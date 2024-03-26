# Foxglove.Authoring

This assembly contains gameobject components for use on entity template prefabs.
The first time a prefab with one of these authoring components is instantiated,
all authoring components on the prefab are converted into ECS components
and attached to a new entity with a Prefab tag.

ECS systems can then spawn instances of that prefab entity by 
