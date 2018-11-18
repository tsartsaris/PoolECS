# PoolECS
Entity Component System (ECS) CrossHybrid style pooling system for Unity for static objects

**Usage**
1. Create an empty game object in the scene and add the PoolECS script
2. Assign the prefab 
3. Adjust options (resizable etc...)
4. Run the scene

Request object from the pool

remember always to  ```using Unity.Entities;``` in your scripts. 

```
using UnityEngine;
using Unity.Entities;
using UnityEngine.UI;

public class PoolBench : MonoBehaviour {

    // the pool
    public PoolECS poolECS;
    
    /// here will be assigned the next available entity(game object)
    Entity entity;

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            for (int i = 0; i < 1000; i++) {
                float x = Random.Range(-100, 100);
                float y = Random.Range(-100, 100);
                float z = Random.Range(-100, 100);
                entity = poolECS.nextEntity(x, y, z);
            }
        }
    }
    
    private void RemoveObject() {
        // call the HideObject method providing the entity you want to be removed
        poolECS.HideObject(entity);
    }
}
```
