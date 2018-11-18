using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;

public class PoolECS : MonoBehaviour {

    [Tooltip("You should adjust your number to not request more entities than those instantiated on awake")]
    [Header("How many entities to initialize")]
    [SerializeField]
    int amount = 10;

    [SerializeField]
    string gameObjectTag = "Default";


    [Tooltip("The ECS prefab should have a Mesh Instance Renderer Component and a poistion component")]
    [Header("The prefab this pool will spawn")]
    [SerializeField]
    GameObject prefab;

    //List<int> indexes;


    /// <summary>
    /// The entity manager is that guy that helps us
    /// instantiate and have access to all entities of the pool
    /// </summary>
    [HideInInspector]
    EntityManager entityManager; //TODO consider dispose him in the end?


    /// <summary>
    /// When we spawn each pooled object we make sure they spawn in a group
    /// This way we can filter all the entities and work only on a subset 
    /// making the system more efficient.
    /// </summary>
    [HideInInspector]
    ComponentGroup m_Group;

    [Tooltip("Group Index is used to optimise the way objects are pooled out of the pool. Must be unique for each pool")]
    [Header("Unique identifier for each pool (int >= 0)")]
    [SerializeField]
    public int groupIndex = -1;


    /// <summary>
    /// this will hold the active world we are rendering objects 
    /// it will be used to avoid errors in case of stop playing in editor
    /// while a script will request an object from the pool
    /// you could further optimise by removing the check if this is created
    /// </summary>
    [HideInInspector]
    World world;


    /// <summary>
    /// If the pool runs out of instantiated objects
    /// and you try to request some more errors will come up
    /// to overcome this we control from the start of the screen the way 
    /// we will handle this situation. To keep things running fast there is 
    /// a delegate GetNextEntity that returns the entity your request 
    /// from the pool. If resize is disabled the Function assigned to the delegate 
    /// is Show() which will not check on every request if the pool has run out
    /// of entities. If resize is true then the Function assigned to the delegate 
    /// is ShowResizable which checks in every request if the pool has run out of entities 
    /// and will spawn some more. 
    /// </summary>
    [Header("Check this to resize the pool on demand")]
    public bool resize = true;


    /// <summary>
    /// a delegate to assign the desirable way of requests
    /// for new entities (game objects) in case our pool 
    /// runs out
    /// </summary>
    /// <param name="x">similar transform.position.x</param>
    /// <param name="y">similar transform.position.y</param>
    /// <param name="z">similar transform.position.z</param>
    /// <returns>Entity entity to control later with EntityManagerif needed</returns>
    public delegate Entity GetNextEntity(float x = 0, float y = 0, float z = 0);
    public GetNextEntity nextEntity;

    private void Awake() {
        // assign the delegate function
        if (resize)
            nextEntity = ShowResizable;
        if (!resize)
            nextEntity = Show;

        //assign the EntityManager
        entityManager = World.Active.GetOrCreateManager<EntityManager>();

        // create the group with the parameters we want to filter entities later
        m_Group = entityManager.CreateComponentGroup(typeof(Indexer), typeof(Visible));


        // assign the active rendering world
        world = World.Active;

        // initialize entities(game objects) in the pool
        // giving the initial amount 
        SpawnObjects(amount);
    }

    /// <summary>
    /// Servers for instantiating at the begin provided the amount
    /// of entities(game objects) from amount 
    /// also if the pool is resizable it will spawn 
    /// the extra entities
    /// </summary>
    /// <param name="extend">amouint of entities to spawn</param>
    private void SpawnObjects(int extend = 10) {
        // temp hold the entities spawned
        NativeArray<Entity> entities = new NativeArray<Entity>(extend, Unity.Collections.Allocator.Temp);
        entityManager.Instantiate(prefab, entities);
        for (int i = 0; i < extend; i++) {
            //entityManager.AddComponent(entities[i], typeof(Frozen)); //TODO add static object option
            // add the index of this pool to filter later and make faster retrieve of an entity
            entityManager.AddSharedComponentData(entities[i], new Indexer { Value = groupIndex });
            // add to filter later only from those not allready spawned
            entityManager.AddSharedComponentData(entities[i], new Visible { Value = 0 });
            // culling of the game object makes render faster
            entityManager.AddComponentData(entities[i], new MeshRenderBounds { Center = new float3(0, 0, 0), Radius = 1.0f });
        }
        // dispose cause we are not animals 
        entities.Dispose();
    }


    /// <summary>
    /// Entities (game objects) are all ready there
    /// we need to just make them visible on the screen
    /// Here we retrieve an entity from the pool 
    /// </summary>
    /// <param name="x">transform.position.x</param>
    /// <param name="y">transform.position.y</param>
    /// <param name="z">transform.position.z</param>
    /// <returns>Entity entity (operate with an entity manager later)</returns>
    private Entity Show(float x, float y, float z) {
        // filtering out entities for faster selection
        m_Group.SetFilter(new Indexer { Value = groupIndex }, new Visible { Value = 0 });
        // put filtered entities in a EntityArray TODO find a better way 
        EntityArray entityArray = m_Group.GetEntityArray();
        // the entity we are going to return will be the first in the list
        // no need to iterate all the array
        Entity entity = entityArray[0];
        // mark as visible to filter it out from the next call
        entityManager.SetSharedComponentData(entity, new Visible { Value = 1 });

        //entityManager.AddComponentData(entity, new Static { });//TODO add static object option
        //entityManager.RemoveComponent(entity, typeof(Frozen)); //TODO add static object option

        // set the position of the entity
        entityManager.AddComponentData(entity, new Position { Value = new float3(x, y, z) });
        // TODO implement this later 
        // normally we should calculate AABB for the object and adjust values before the call
        // at the moment we should assign the culling sphere center at where the object is 
        // improves performance
        entityManager.SetComponentData(entity, new MeshRenderBounds { Center = new float3(x, y, z), Radius = 1.0f });
        // magic
        return entity;
    }


    /// <summary>
    /// same as Show but adds an extra check
    /// to see if when filtering and getting all available entities
    /// we have at least one to make available
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    private Entity ShowResizable(float x, float y, float z) {
        m_Group.SetFilter(new Indexer { Value = groupIndex }, new Visible { Value = 0 });

        // check if the group has any entities available
        // if not it will spawn objects 
        // TODO add a extend variable to define how many will be spawned if needed
        if (!(m_Group.CalculateLength() > 0)) {
            SpawnObjects(amount);
        }
        // put filtered entities in a EntityArray TODO find a better way ?BUFFER MAYBE
        EntityArray entityArray = m_Group.GetEntityArray();
        // the entity we are going to return will be the first in the list
        // no need to iterate all the array
        Entity entity = entityArray[0];
        // mark as visible to filter it out from the next call
        entityManager.SetSharedComponentData(entity, new Visible { Value = 1 });


        //entityManager.AddComponentData(entity, new Static { });//TODO add static object option
        //entityManager.RemoveComponent(entity, typeof(Frozen)); //TODO add static object option


        // set the position of the entity
        entityManager.AddComponentData(entity, new Position { Value = new float3(x, y, z) });
        
        // TODO implement this later 
        // normally we should calculate AABB for the object and adjust values before the call
        // at the moment we should assign the culling sphere center at where the object is 
        // improves performance
        entityManager.SetComponentData(entity, new MeshRenderBounds { Center = new float3(x, y, z), Radius = 1.0f });
        // magic
        return entity;
    }

    public void HideObject(Entity entity) {
        // avoid error in case of scene change where the world 
        // could not be available but objects are keep 
        // sending requests
        if (!world.IsCreated)
            return;

        // mark as not visible for later filtering
        entityManager.SetSharedComponentData(entity, new Visible { Value = 0 });
        //remove the position
        entityManager.RemoveComponent(entity, typeof(Position));
        entityManager.RemoveComponent(entity, typeof(VisibleLocalToWorld));
        entityManager.RemoveComponent(entity, typeof(LocalToWorld));
        entityManager.CompleteAllJobs();
    }


#if UNITY_EDITOR
    private void OnValidate() {
        // validate the change of resize boolean 
        // in order to control how new requests will be handled
        if (resize)
            nextEntity = ShowResizable;
        if (!resize)
            nextEntity = Show;

        // validate the group index number.
        // iterate all instances of PoolECS 
        // if we find it then display error
        foreach (PoolECS go in FindObjectsOfType<PoolECS>()) {
            if (go == this)
                continue;
            if (go.groupIndex == groupIndex) {
                Debug.LogErrorFormat("Groupn Index {0} has been used in another pool, please choose another index!", groupIndex);

                // ping the object to highlight in hierarchy
                UnityEditor.EditorGUIUtility.PingObject(gameObject);
                // do not allow play mode since pools are not going to work
                UnityEditor.EditorApplication.isPlaying = false;


            }
        }
       
    }
#endif


}
