using System.Diagnostics;
using Android.Media;

namespace BubbleTea.App;

internal sealed class DrinkAudio
{
    private MediaPlayer? _player;
    private bool _failed;

    public void SetDrinking(bool drinking)
    {
        if (_failed)
            return;

        try
        {
            if (drinking)
            {
                _player ??= MediaPlayer.Create(Android.App.Application.Context, Resource.Raw.sip);
                if (_player is null)
                {
                    _failed = true;
                    return;
                }
                _player.Looping = true;
                _player.Start();
            }
            else if (_player is not null)
            {
                _player.Pause();
                _player.SeekTo(0);
            }
        }
        catch (Java.Lang.RuntimeException ex)
        {
            Debug.WriteLine(ex);
            _failed = true;
            Release();
        }
    }

    public void Release()
    {
        _player?.Release();
        _player?.Dispose();
        _player = null;
    }
}
