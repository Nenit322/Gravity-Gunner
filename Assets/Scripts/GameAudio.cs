using UnityEngine;

public static class GameAudio
{

    public static void PlayAt(string sound, Vector3 position)
    {

        if (!Application.isPlaying) return;

        AudioClip clip = Resources.Load<AudioClip>("Audio/" + sound);

        if (clip != null) AudioSource.PlayClipAtPoint(clip, position, 0.25f);

    }

}

