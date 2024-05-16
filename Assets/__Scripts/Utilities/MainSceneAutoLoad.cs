#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
#endif

public class MainSceneAutoLoad : MonoBehaviour
{
#if UNITY_EDITOR
    private void Start()
    {
        //Automatically load the base scene if it isn't the active one
        //Allows the app to run properly when starting from the editor view of a different scene
        if(SceneManager.GetActiveScene() != SceneManager.GetSceneByBuildIndex(0))
        {
            SceneManager.LoadScene(0, LoadSceneMode.Single);
        }
    }
#endif
}