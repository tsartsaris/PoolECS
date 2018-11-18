using System;
using Unity.Entities;

/// <summary>
/// shared component data used by EntityManager
/// to filter entities belonging to different groups
/// values = 0 -> inf
/// values are set in the inspector
/// </summary>

[Serializable]
public struct Indexer : ISharedComponentData {
    public int Value;
}

[UnityEngine.DisallowMultipleComponent]
public class IndexingComponent : SharedComponentDataWrapper<Indexer> {
}

