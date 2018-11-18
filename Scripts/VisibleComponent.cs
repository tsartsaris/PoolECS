using System;
using Unity.Entities;

/// <summary>
/// shared component data used by EntityManager
/// for filtering over groups of pooled entities
/// 0 = Not Visible 1 = Visible
/// values are set automatically on spawn despawn
/// </summary>

[Serializable]
public struct Visible : ISharedComponentData {
    public int Value;
}

[UnityEngine.DisallowMultipleComponent]
public class VisibleComponent : SharedComponentDataWrapper<Visible> {
}

