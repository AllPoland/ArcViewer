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
    [SerializeField] private List<LightHandler> lightHandlers;


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
            for(int i = 0; i < ring.lightHandlers.Count; i++)
            {
                SerializedObject serialized = new SerializedObject(ring.lightHandlers[i]);
                serialized.FindProperty("id").intValue = i + 1 + (ring.id * ring.lightHandlers.Count);
                serialized.ApplyModifiedProperties();
            }
        }
        
        EditorSceneManager.MarkSceneDirty(scene);
    }
#endif
}