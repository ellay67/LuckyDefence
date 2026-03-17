namespace LuckyDefense.Services;

/// <summary>
/// Generates simple WAV sound effects programmatically and plays them.
/// No external audio files needed.
/// </summary>
public class SoundService
{
    private static SoundService? _instance;
    public static SoundService Instance => _instance ??= new SoundService();

    private readonly Dictionary<string, byte[]> _sounds = new();
    private bool _initialized;

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        // Generate all sound effects
        _sounds["shoot_common"] = GenerateTone(800, 0.06f, 0.3f);
        _sounds["shoot_rare"] = GenerateTone(1000, 0.08f, 0.3f, waveType: 1);
        _sounds["shoot_epic"] = GenerateSweep(600, 1200, 0.1f, 0.35f);
        _sounds["shoot_legendary"] = GenerateSweep(400, 1400, 0.12f, 0.4f);
        _sounds["enemy_die"] = GenerateSweep(600, 200, 0.1f, 0.25f);
        _sounds["enemy_leak"] = GenerateTone(200, 0.15f, 0.5f);
        _sounds["buy_unit"] = GenerateSweep(400, 800, 0.08f, 0.3f);
        _sounds["upgrade_luck"] = GenerateSweep(500, 1500, 0.15f, 0.3f);
        _sounds["merge"] = GenerateChord(new[] { 523, 659, 784 }, 0.2f, 0.3f);
        _sounds["wave_start"] = GenerateSweep(300, 600, 0.2f, 0.4f);
        _sounds["epic_pull"] = GenerateChord(new[] { 523, 659, 784, 1047 }, 0.4f, 0.35f);
        _sounds["legendary_pull"] = GenerateChord(new[] { 440, 554, 659, 880, 1109 }, 0.5f, 0.4f);
        _sounds["game_over"] = GenerateSweep(800, 200, 0.4f, 0.4f);
        _sounds["win"] = GenerateChord(new[] { 523, 659, 784, 1047 }, 0.6f, 0.35f);
    }

    public void Play(string soundName)
    {
        if (!Preferences.Get("SoundEnabled", true)) return;
        if (!_sounds.TryGetValue(soundName, out var wavData)) return;

        Task.Run(() =>
        {
            try
            {
                string tempPath = Path.Combine(FileSystem.CacheDirectory, $"{soundName}.wav");
                if (!File.Exists(tempPath))
                    File.WriteAllBytes(tempPath, wavData);

#if ANDROID
                var player = new Android.Media.MediaPlayer();
                player.SetDataSource(tempPath);
                player.Prepare();
                player.Start();
                player.Completion += (s, e) =>
                {
                    player.Release();
                    player.Dispose();
                };
#endif
            }
            catch { /* Ignore audio errors */ }
        });
    }

    private byte[] GenerateTone(int frequency, float duration, float volume, int waveType = 0)
    {
        int sampleRate = 22050;
        int samples = (int)(sampleRate * duration);
        var pcm = new short[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1f - (float)i / samples; // Fade out
            float sample = waveType switch
            {
                0 => MathF.Sin(2 * MathF.PI * frequency * t), // Sine
                1 => (MathF.Sin(2 * MathF.PI * frequency * t) > 0 ? 1f : -1f) * 0.5f, // Square (softer)
                _ => MathF.Sin(2 * MathF.PI * frequency * t)
            };
            pcm[i] = (short)(sample * envelope * volume * 32767);
        }

        return CreateWav(pcm, sampleRate);
    }

    private byte[] GenerateSweep(int startFreq, int endFreq, float duration, float volume)
    {
        int sampleRate = 22050;
        int samples = (int)(sampleRate * duration);
        var pcm = new short[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float progress = (float)i / samples;
            float freq = startFreq + (endFreq - startFreq) * progress;
            float envelope = 1f - progress;
            float sample = MathF.Sin(2 * MathF.PI * freq * t);
            pcm[i] = (short)(sample * envelope * volume * 32767);
        }

        return CreateWav(pcm, sampleRate);
    }

    private byte[] GenerateChord(int[] frequencies, float duration, float volume)
    {
        int sampleRate = 22050;
        int samples = (int)(sampleRate * duration);
        var pcm = new short[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float progress = (float)i / samples;
            float envelope = progress < 0.1f ? progress / 0.1f : 1f - (progress - 0.1f) / 0.9f;
            float sample = 0;
            foreach (int freq in frequencies)
                sample += MathF.Sin(2 * MathF.PI * freq * t);
            sample /= frequencies.Length;
            pcm[i] = (short)(sample * envelope * volume * 32767);
        }

        return CreateWav(pcm, sampleRate);
    }

    private byte[] CreateWav(short[] pcm, int sampleRate)
    {
        int dataSize = pcm.Length * 2;
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        // WAV header
        w.Write("RIFF"u8);
        w.Write(36 + dataSize);
        w.Write("WAVE"u8);
        w.Write("fmt "u8);
        w.Write(16);           // Chunk size
        w.Write((short)1);     // PCM
        w.Write((short)1);     // Mono
        w.Write(sampleRate);
        w.Write(sampleRate * 2); // Byte rate
        w.Write((short)2);     // Block align
        w.Write((short)16);    // Bits per sample
        w.Write("data"u8);
        w.Write(dataSize);

        foreach (var sample in pcm)
            w.Write(sample);

        return ms.ToArray();
    }
}
