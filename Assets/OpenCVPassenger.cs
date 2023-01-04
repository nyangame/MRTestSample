using Mediapipe.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class OpenCVPassenger : ImageSource
{
    [SerializeField] OpenCVCtrl _ctrl;

    private Texture2D _outputTexture;

    [SerializeField]
    private ResolutionStruct[] _defaultAvailableResolutions = new ResolutionStruct[] {
      new ResolutionStruct(640, 480, 30),
    };

    //    public override double frameRate => 0;

    public override string sourceName => "OpenCV";

    public override string[] sourceCandidateNames => new string[]{"OpenCV"};

    public override ResolutionStruct[] availableResolutions => _defaultAvailableResolutions;

    public override bool isPrepared => _outputTexture != null;

    private bool _isPlaying = false;
    public override bool isPlaying => _isPlaying;


    public override void SelectSource(int sourceId)
    {
        if (sourceId < 0 || sourceId > 0)
        {
            throw new ArgumentException($"Invalid source ID: {sourceId}");
        }

        //
    }

    public override IEnumerator Play()
    {
        if (_ctrl == null)
        {
            throw new InvalidOperationException("Image is not selected");
        }
        if (isPlaying)
        {
            yield break;
        }
        if(!_ctrl.isPrepare)
        {
            yield return null;
        }

        var source = _ctrl.GetScreenTexture();
        resolution = new ResolutionStruct(source.width, source.height, 30);
        InitializeOutputTexture(source);
        _isPlaying = true;
        yield return null;
    }

    public override IEnumerator Resume()
    {
        if (!isPrepared)
        {
            throw new InvalidOperationException("Image is not prepared");
        }
        _isPlaying = true;

        yield return null;
    }

    public override void Pause()
    {
        _isPlaying = false;
    }
    public override void Stop()
    {
        _isPlaying = false;
        _outputTexture = null;
    }

    public override Texture GetCurrentTexture()
    {
        if(!_isPlaying) return null;

        InitializeOutputTexture(_ctrl.GetScreenTexture());
        return _outputTexture;
    }

    private ResolutionStruct GetDefaultResolution()
    {
        var resolutions = availableResolutions;

        return (resolutions == null || resolutions.Length == 0) ? new ResolutionStruct() : resolutions[0];
    }
    private void InitializeOutputTexture(Texture src)
    {
        _outputTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);

        Texture resizedTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        // TODO: assert ConvertTexture finishes successfully
        var _ = Graphics.ConvertTexture(src, resizedTexture);

        var currentRenderTexture = RenderTexture.active;
        var tmpRenderTexture = new RenderTexture(resizedTexture.width, resizedTexture.height, 32);
        Graphics.Blit(resizedTexture, tmpRenderTexture);
        RenderTexture.active = tmpRenderTexture;

        var rect = new UnityEngine.Rect(0, 0, _outputTexture.width, _outputTexture.height);
        _outputTexture.ReadPixels(rect, 0, 0);
        _outputTexture.Apply();

        RenderTexture.active = currentRenderTexture;
        GetDefaultResolution();
    }
}
