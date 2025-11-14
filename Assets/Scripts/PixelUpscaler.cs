using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PixelUpscaler : MonoBehaviour
{
    public RenderTexture source;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (source != null)
        {
            Graphics.Blit(source, dest);
        }
    }
}
