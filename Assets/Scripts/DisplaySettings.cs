using UnityEngine;

// Display mode belongs to the application, so restarting a round keeps the chosen mode.
public class DisplaySettings : MonoBehaviour
{

    static bool initialized;

    void Start()
    {

        if (Application.isEditor || Application.isBatchMode || initialized)
        {

            return;

        }

        initialized = true;

        SetFullscreen(true);

    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.F11))
        {

            Toggle();

        }

    }

    public void Toggle()
    {

        if (Application.isEditor || Application.isBatchMode)
        {

            return;

        }

        SetFullscreen(!Screen.fullScreen);

    }

    static void SetFullscreen(bool fullscreen)
    {

        int width = Mathf.Max(1, Display.main.systemWidth);

        int height = Mathf.Max(1, Display.main.systemHeight);

        if (fullscreen)
        {

            Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);

        }

        else
        {

            width = Mathf.Min(1280, width);

            height = Mathf.Min(720, height);

            Screen.SetResolution(width, height, FullScreenMode.Windowed);

        }

    }

}

