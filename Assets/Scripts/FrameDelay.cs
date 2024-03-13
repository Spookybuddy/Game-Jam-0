using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

public class FrameDelay : MonoBehaviour
{
    //The camera used to capture the frames
    [SerializeField]
    private Camera renderCamera;
    //The texture with camera's current frame
    [SerializeField]
    private RenderTexture renderTexture;
    //The texture output at a frame delay
    [SerializeField]
    private RenderTexture outputTexture;

    private Texture2D current;
    private Texture2D previous;

    //Adds to renderpipeline framedelay function
    void Start()
    {
        current = new Texture2D(renderTexture.width, renderTexture.height);
        previous = new Texture2D(outputTexture.width, outputTexture.height);
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    //Give a frame delay only to the camera that matches the render cam
    void OnEndCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        if (cam == renderCamera) {
            //Move current to previous, giving a frame delay effect
            Graphics.CopyTexture(current, previous);
            current.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            current.Apply();

            RenderTexture.active = renderTexture;
            Graphics.Blit(previous, outputTexture);
            RenderTexture.active = null;
        }
    }

    //Scale the render textures and the texture2Ds
    public void UpdateGraphicsSettings(int H, int W)
    {
        renderTexture.height = H;
        renderTexture.width = W;
        outputTexture.height = H;
        outputTexture.width = W;
        current = new Texture2D(renderTexture.width, renderTexture.height);
        previous = new Texture2D(outputTexture.width, outputTexture.height);
    }

    //Remove function from renderpipeline
    void OnDestroy()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }
}