using System;
using System.Collections;
using B83.Image.GIF;
using UnityEngine;

public class AnimatedAvatar : IDisposable
{
    public readonly bool IsAnimated;
    public readonly RenderTexture TargetTexture;

    private readonly Texture2D _originalTexture;
    private readonly GIFImage _gifImage;


    public AnimatedAvatar(Texture2D originalTexture, GIFImage gifImage, RenderTexture target)
    {
        IsAnimated = gifImage != null;
        _originalTexture = originalTexture;
        _gifImage = gifImage;
        TargetTexture = target;
    }


    public static AnimatedAvatar StaticAvatar(Texture2D texture, RenderTexture target) => new AnimatedAvatar(texture, null, target);


    public IEnumerator PlaybackCoroutine()
    {
        if(!IsAnimated)
        {
            Graphics.Blit(_originalTexture, TargetTexture);
            yield break;
        }

        if(_gifImage.imageData.Count == 0) yield break;

        Color32[] colors = _originalTexture.GetPixels32();
        while(true)
        {
            foreach(IGIFRenderingBlock frame in _gifImage.imageData)
            {
                frame.DrawTo(colors, _originalTexture.width, _originalTexture.height);
                _originalTexture.SetPixels32(colors);
                _originalTexture.Apply();

                Graphics.Blit(_originalTexture, TargetTexture);

                yield return new WaitForSeconds(frame.graphicControl.fdelay);
                frame.Dispose(colors, _originalTexture.width, _originalTexture.height);
            }
        }
    }


    public void SetFirstFrame()
    {
        if(!IsAnimated || _gifImage.imageData.Count == 0)
        {
            return;
        }

        Color32[] colors = _originalTexture.GetPixels32();
        IGIFRenderingBlock frame = _gifImage.imageData[0];

        frame.DrawTo(colors, _originalTexture.width, _originalTexture.height);
        _originalTexture.SetPixels32(colors);
        _originalTexture.Apply();

        Graphics.Blit(_originalTexture, TargetTexture);
    }


    public void Dispose()
    {
        GameObject.Destroy(_originalTexture);
    }
}