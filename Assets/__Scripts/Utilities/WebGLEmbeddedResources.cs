#if UNITY_EDITOR
using UnityEditor;

public class WebGLEditorScript
{
    //This is a script to enable embedded resources from DLLs in WebGL
    //Unity disables this by default because "muh file size"
    //I need this to be enabled for the Winista.MimeDetect plugin to work
    //If, for some reason, the player setting gets reverted,
    //click "WebGL" in the top bar of the editor and click "Enable Embedded Resources"

    [MenuItem("WebGL/Enable Embedded Resources")]
    public static void EnableEmbeddedResources()
    {
        PlayerSettings.WebGL.useEmbeddedResources = true;
    }
}
#endif