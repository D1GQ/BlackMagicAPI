using BetterVoiceDetection;
using System.Reflection;
using UnityEngine;

namespace BlackMagicAPI.Helpers;

internal class WavUtility
{
    public static AudioClip? ToAudioClip(byte[] wavBytes)
    {
        if (wavBytes.Length < 44)
        {
            BMAPlugin.Log.LogError("Invalid WAV file (too short)");
            return null;
        }

        if (wavBytes[0] != 'R' || wavBytes[1] != 'I' || wavBytes[2] != 'F' || wavBytes[3] != 'F')
        {
            BMAPlugin.Log.LogError("Invalid WAV file (missing RIFF header)");
            return null;
        }

        int sampleRate = BitConverter.ToInt32(wavBytes, 24);
        int channels = BitConverter.ToInt16(wavBytes, 22);
        int samples = BitConverter.ToInt32(wavBytes, 40) / 2;

        int offset = 36;
        while (offset < wavBytes.Length - 8)
        {
            if (wavBytes[offset] == 'd' && wavBytes[offset + 1] == 'a' &&
                wavBytes[offset + 2] == 't' && wavBytes[offset + 3] == 'a')
            {
                offset += 4;
                int dataSize = BitConverter.ToInt32(wavBytes, offset);
                offset += 4;
                break;
            }
            offset++;
        }

        float[] floatData = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            short sample = BitConverter.ToInt16(wavBytes, offset + i * 2);
            floatData[i] = sample / 32768f;
        }

        Type audioClipType = typeof(AudioClip);

        // Define the parameter types for the Create method
        Type[] parameterTypes =
        [
            typeof(string),    // name
            typeof(int),       // lengthSamples
            typeof(int),       // channels
            typeof(int),       // frequency
            typeof(bool)       // stream
        ];

        MethodInfo createMethod = audioClipType.GetMethod(
            "Create",
            BindingFlags.Public | BindingFlags.Static,
            null,
            parameterTypes,
            null
        );

        if (createMethod == null)
        {
            throw new MissingMethodException("AudioClip.Create method not found!");
        }

        AudioClip clip = (AudioClip)createMethod.Invoke(
        null,
            new object[] { "LoadedWav", samples / channels, channels, sampleRate, false }
        );
        MethodInfo setDataMethod = audioClipType.GetMethod(
            "SetData",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            new Type[] { typeof(float[]), typeof(int) },
            null
        );

        if (setDataMethod == null)
        {
            throw new MissingMethodException("AudioClip.SetData method not found!");
        }

        setDataMethod.Invoke(clip, new object[] { floatData, 0 });

        return clip;
    }
}
