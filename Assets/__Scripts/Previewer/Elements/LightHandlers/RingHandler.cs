using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
#endif

public class RingHandler : MonoBehaviour
{
    [SerializeField] private bool bigRing;
    [SerializeField] private int id;

#if UNITY_EDITOR
    [SerializeField] private int lightIdOffset;
    [SerializeField] private List<LightHandler> lightHandlers;

    [Space]
    [SerializeField] private float defaultZoom = 1f;
    [SerializeField] private float defaultStep = 0f;
#endif


    public void UpdateRingRotations(RingRotationEventArgs eventArgs)
    {
        if(eventArgs.affectBigRings != bigRing)
        {
            return;
        }

        RingRotationEvent current = null;
        for(int i = eventArgs.currentEventIndex; i >= 0; i--)
        {
            //Because of how prop works, we might need to go back
            //and find the first event actually affecting this ring
            if(eventArgs.events[i].StartInfluenceTime(id) <= TimeManager.CurrentTime)
            {
                current = eventArgs.events[i];
                break;
            }
        }

        if(current == null)
        {
            //No rotation event has influenced this ring, use defaults
            float defaultAngle = bigRing ? RingManager.BigRingStartAngle : RingManager.SmallRingStartAngle;
            float defaultStep = bigRing ? RingManager.BigRingStartStep : RingManager.SmallRingStartStep;
            SetRotation(defaultAngle + (defaultStep * id));
            return;
        }

        SetRotation(current.GetRingAngle(TimeManager.CurrentTime, id));
    }


    private void SetRotation(float angle)
    {
        Vector3 eulerAngles = transform.localEulerAngles;
        eulerAngles.z = angle % 360;
        transform.localEulerAngles = eulerAngles;
    }


    private void OnEnable()
    {
        RingManager.OnRingRotationsChanged += UpdateRingRotations;
    }


    private void OnDisable()
    {
        RingManager.OnRingRotationsChanged -= UpdateRingRotations;
    }


#if UNITY_EDITOR
    [MenuItem("Environment/SetRingLightIDs")]
    public static void SetRingLightIDs()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();

        List<RingHandler> ringHandlers = new List<RingHandler>();
        foreach(GameObject gameObject in rootObjects)
        {
            ringHandlers.AddRange(gameObject.GetComponentsInChildren<RingHandler>());
        }

        foreach(RingHandler ring in ringHandlers)
        {
            //Count each event type so they can be handled separately
            Dictionary<LightEventType, int> typeCounts = new Dictionary<LightEventType, int>();
            foreach(LightHandler lightHandler in ring.lightHandlers)
            {
                if(typeCounts.TryGetValue(lightHandler.type, out int currentCount))
                {
                    typeCounts[lightHandler.type] = currentCount + 1;
                }
                else typeCounts[lightHandler.type] = 1;
            }

            Dictionary<LightEventType, int> typeIndices = new Dictionary<LightEventType, int>();
            foreach(LightHandler lightHandler in ring.lightHandlers)
            {
                SerializedObject serialized = new SerializedObject(lightHandler);

                int i = typeIndices.TryGetValue(lightHandler.type, out int index) ? index : 0;
                int ringID = ring.id + ring.lightIdOffset;

                serialized.FindProperty("id").intValue = i + 1 + (ringID * typeCounts[lightHandler.type]);
                serialized.ApplyModifiedProperties();

                typeIndices[lightHandler.type] = i + 1;
            }
        }
        
        EditorSceneManager.MarkSceneDirty(scene);
    }


    [MenuItem("Environment/SetRingPositions")]
    public static void SetRingPositions()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();

        List<RingHandler> ringHandlers = new List<RingHandler>();

        foreach(GameObject gameObject in rootObjects)
        {
            ringHandlers.AddRange(gameObject.GetComponentsInChildren<RingHandler>());
        }

        foreach(RingHandler ring in ringHandlers)
        {
            float zPos = ring.defaultZoom * ring.id;
            float zRot = ring.defaultStep * ring.id;
            
            ring.transform.localPosition = Vector3.forward * zPos;
            ring.transform.localEulerAngles = Vector3.forward * zRot;
        }
        
        EditorSceneManager.MarkSceneDirty(scene);
    }
#endif
}