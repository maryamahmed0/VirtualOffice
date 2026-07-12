using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;


public class MeetingAudioMixer : MonoBehaviour
{
    public static MeetingAudioMixer Instance { get; private set; }

    private const string AI_ENDPOINT_URL = "https://localhost:7080";
    [SerializeField] private int sampleRate = 16000;

    private class ChunkBucket
    {
        public readonly Dictionary<string, float[]> BySpeaker = new Dictionary<string, float[]>();
    }

    private string _activeRoomId;
    private string _activeMeetingId; 
    private bool _sessionActive;
    private readonly Dictionary<int, ChunkBucket> _buckets = new Dictionary<int, ChunkBucket>();
    private readonly HashSet<string> _speakersThisSession = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }



    public void StartSession(string roomId)
    {
        if (_sessionActive) return;

        _activeRoomId = roomId;
        _activeMeetingId = roomId;
        _sessionActive = true;
        _buckets.Clear();
        _speakersThisSession.Clear();

        Debug.Log($"[AUDIO MIX] Session started for room {roomId}");
    }

    public void AddChunk(string roomId, string speakerId, int chunkIndex, byte[] pcm16, bool isLastChunk)
    {
        if (!_sessionActive || roomId != _activeRoomId)
        {
            Debug.LogWarning($"[AUDIO MIX] Dropped chunk {chunkIndex} from {speakerId} � no active session for room '{roomId}' (active='{_activeRoomId}', sessionActive={_sessionActive}).");
            return;
        }

        float[] samples = Pcm16BytesToFloats(pcm16);

        if (!_buckets.TryGetValue(chunkIndex, out var bucket))
        {
            bucket = new ChunkBucket();
            _buckets[chunkIndex] = bucket;
        }

        bucket.BySpeaker[speakerId] = samples;
        _speakersThisSession.Add(speakerId);

        Debug.Log($"[AUDIO MIX] Buffered bucket {chunkIndex} � {bucket.BySpeaker.Count} speaker(s): [{string.Join(",", bucket.BySpeaker.Keys)}] (session has {_buckets.Count} buckets total so far)");
    }

    public void EndSession(string roomId)
    {
        if (!_sessionActive || roomId != _activeRoomId)
        {
            Debug.LogWarning($"[AUDIO MIX] EndSession('{roomId}') ignored � sessionActive={_sessionActive}, active='{_activeRoomId}'.");
            return;
        }

        Debug.Log($"[AUDIO MIX] EndSession for room {roomId} � {_buckets.Count} bucket(s) buffered, {_speakersThisSession.Count} speaker(s) total. Building final mixed file...");

        StartCoroutine(BuildAndUploadFinalFileThenSummarize());
    }

    private IEnumerator BuildAndUploadFinalFileThenSummarize()
    {
        yield return new WaitForSeconds(0.5f);

        _sessionActive = false;

        byte[] finalWav = BuildFinalMixedWav();
        var allSpeakers = _speakersThisSession.ToList();
        _buckets.Clear();

        if (finalWav == null)
        {
            Debug.LogWarning("[AUDIO MIX] No audio was buffered for this meeting � skipping upload and summary.");
            yield break;
        }

        yield return StartCoroutine(UploadFinalFile(finalWav, allSpeakers));
        yield return StartCoroutine(GenerateAndSaveSummary());
    }

    private byte[] BuildFinalMixedWav()
    {
        var orderedIndices = _buckets.Keys.OrderBy(i => i).ToList();
        if (orderedIndices.Count == 0) return null;

        var windowTracks = new List<float[]>(orderedIndices.Count);
        int totalLength = 0;

        foreach (int idx in orderedIndices)
        {
            float[] mixed = MixBucket(_buckets[idx]);
            windowTracks.Add(mixed);
            totalLength += mixed.Length;
        }

        float[] fullTrack = new float[totalLength];
        int writeOffset = 0;
        foreach (var window in windowTracks)
        {
            Array.Copy(window, 0, fullTrack, writeOffset, window.Length);
            writeOffset += window.Length;
        }

        Debug.Log($"[AUDIO MIX] Final track built: {orderedIndices.Count} window(s), {totalLength} samples (~{totalLength / (float)sampleRate:F1}s).");

        return ConvertToWav(fullTrack, sampleRate);
    }

    private float[] MixBucket(ChunkBucket bucket)
    {
        int maxLen = bucket.BySpeaker.Values.Max(s => s.Length);
        float[] mixed = new float[maxLen];

        foreach (var speakerSamples in bucket.BySpeaker.Values)
            for (int i = 0; i < speakerSamples.Length; i++)
                mixed[i] += speakerSamples[i];

        for (int i = 0; i < mixed.Length; i++)
            mixed[i] = Mathf.Clamp(mixed[i], -1f, 1f);

        return mixed;
    }

    private IEnumerator UploadFinalFile(byte[] wavData, List<string> speakerIds)
    {
        if (string.IsNullOrEmpty(_activeMeetingId))
        {
            Debug.LogWarning("[AUDIO MIX] UploadFinalFile skipped � _activeMeetingId is empty.");
            yield break;
        }

        Debug.Log($"[AUDIO MIX] Uploading final merged meeting file | wavBytes={wavData.Length} | speakers=[{string.Join(",", speakerIds)}]");

        var form = new WWWForm();
        form.AddField("roomId", _activeRoomId);
        form.AddField("orgCode", GameSessionData.Instance != null ? GameSessionData.Instance.OrgCode : "");
        form.AddField("speakerId", string.Join(",", speakerIds));
        form.AddField("chunkIndex", "0");
        form.AddField("isLastChunk", "true"); // this IS the whole meeting in one shot
        form.AddBinaryData("file", wavData, "meeting_full.wav", "audio/wav");

        string url = $"{AI_ENDPOINT_URL}/api/AiMeeting/{_activeMeetingId}/transcribe-secure";
        using var req = UnityWebRequest.Post(url, form);
        req.certificateHandler = new BypassCertificate();
        req.SetRequestHeader("Authorization", "Bearer " + AuthBridge.Token);

        yield return req.SendWebRequest();

        Debug.Log($"[AUDIO MIX] Final file upload response code={req.responseCode} body={req.downloadHandler?.text}");

        if (req.result == UnityWebRequest.Result.Success)
            Debug.Log($"[AUDIO MIX] Final merged file sent for meeting {_activeMeetingId} | speakers=[{string.Join(",", speakerIds)}]");
        else
            Debug.LogWarning($"[AUDIO MIX] Final file upload FAILED {req.error} | {req.downloadHandler?.text}");
    }


    [Serializable] private class TranscriptApiResponse { public TranscriptData data; public bool isSuccess; public string message; }
    [Serializable] private class TranscriptData { public string transcript; }
    [Serializable] private class GenerateSummaryRequest { public int meeting_id; public string transcript; public string meeting_title; public string[] attendees; public string language; }
    [Serializable] private class SaveSummaryRequest { public string transcript; public string signature; }

    private IEnumerator GenerateAndSaveSummary()
    {
        if (string.IsNullOrEmpty(_activeMeetingId))
        {
            Debug.LogWarning("[AUDIO MIX] No meetingId, skipping summary generation.");
            yield break;
        }

        if (!int.TryParse(_activeMeetingId, out int numericMeetingId))
        {
            Debug.LogError($"[AUDIO MIX] generate-summary needs an int meeting_id but '{_activeMeetingId}' isn't numeric. " +
                            "Ask backend how the numeric meeting_id should be obtained, then fix this.");
            yield break;
        }

        string transcriptText;
        using (var transcriptReq = UnityWebRequest.Get($"{AI_ENDPOINT_URL}/api/AiMeeting/meetings/{_activeMeetingId}/transcript"))
        {
            transcriptReq.certificateHandler = new BypassCertificate();
            transcriptReq.SetRequestHeader("Authorization", "Bearer " + AuthBridge.Token);
            yield return transcriptReq.SendWebRequest();

            if (transcriptReq.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[AUDIO MIX] transcript fetch FAILED {transcriptReq.error} | {transcriptReq.downloadHandler?.text}");
                yield break;
            }

            transcriptText = ExtractTranscriptText(transcriptReq.downloadHandler.text);
            if (string.IsNullOrEmpty(transcriptText))
            {
                Debug.LogWarning($"[AUDIO MIX] transcript came back empty, raw={transcriptReq.downloadHandler.text}");
                yield break;
            }
        }

        var genPayload = new GenerateSummaryRequest
        {
            meeting_id = numericMeetingId,
            transcript = transcriptText,
            meeting_title = "Meeting " + _activeRoomId,
            attendees = _speakersThisSession.ToArray(),
            language = "en" 
        };

        string genJson = JsonUtility.ToJson(genPayload);
        byte[] genBody = System.Text.Encoding.UTF8.GetBytes(genJson);

        using (var genReq = new UnityWebRequest($"{AI_ENDPOINT_URL}/api/AiMeeting/generate-summary", UnityWebRequest.kHttpVerbPOST))
        {
            genReq.uploadHandler = new UploadHandlerRaw(genBody);
            genReq.downloadHandler = new DownloadHandlerBuffer();
            genReq.SetRequestHeader("Content-Type", "application/json");
            genReq.certificateHandler = new BypassCertificate();
            genReq.SetRequestHeader("Authorization", "Bearer " + AuthBridge.Token);

            yield return genReq.SendWebRequest();

            if (genReq.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[AUDIO MIX] generate-summary FAILED {genReq.error} | {genReq.downloadHandler?.text}");
                yield break;
            }

            Debug.Log($"[AUDIO MIX] Summary generated for meeting {_activeMeetingId}");
        }

        
        var savePayload = new SaveSummaryRequest { transcript = transcriptText, signature = "" };
        string saveJson = JsonUtility.ToJson(savePayload);
        byte[] saveBody = System.Text.Encoding.UTF8.GetBytes(saveJson);

        using var saveReq = new UnityWebRequest($"{AI_ENDPOINT_URL}/api/AiMeeting/{_activeMeetingId}/save-summary", UnityWebRequest.kHttpVerbPOST);
        saveReq.uploadHandler = new UploadHandlerRaw(saveBody);
        saveReq.downloadHandler = new DownloadHandlerBuffer();
        saveReq.SetRequestHeader("Content-Type", "application/json");
        saveReq.certificateHandler = new BypassCertificate();
        saveReq.SetRequestHeader("Authorization", "Bearer " + AuthBridge.Token);

        yield return saveReq.SendWebRequest();

        if (saveReq.result == UnityWebRequest.Result.Success)
            Debug.Log($"[AUDIO MIX] Summary saved for meeting {_activeMeetingId}");
        else
            Debug.LogWarning($"[AUDIO MIX] save-summary FAILED {saveReq.error} | {saveReq.downloadHandler?.text}");
    }

    private static string ExtractTranscriptText(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        try
        {
            var wrapped = JsonUtility.FromJson<TranscriptApiResponse>(raw);
            if (wrapped?.data != null && !string.IsNullOrEmpty(wrapped.data.transcript))
                return wrapped.data.transcript;
        }
        catch { /* fall through to raw text */ }
        return raw.Trim().Trim('"');
    }


    private static float[] Pcm16BytesToFloats(byte[] bytes)
    {
        int count = bytes.Length / 2;
        float[] samples = new float[count];
        for (int i = 0; i < count; i++)
        {
            short val = (short)(bytes[i * 2] | (bytes[i * 2 + 1] << 8));
            samples[i] = val / 32767f;
        }
        return samples;
    }

    private static byte[] ConvertToWav(float[] samples, int sampleRate)
    {
        int byteCount = samples.Length * 2;
        byte[] wav = new byte[44 + byteCount];

        WriteString(wav, 0, "RIFF");
        WriteInt(wav, 4, 36 + byteCount);
        WriteString(wav, 8, "WAVE");
        WriteString(wav, 12, "fmt ");
        WriteInt(wav, 16, 16);
        WriteShort(wav, 20, 1);
        WriteShort(wav, 22, 1);
        WriteInt(wav, 24, sampleRate);
        WriteInt(wav, 28, sampleRate * 2);
        WriteShort(wav, 32, 2);
        WriteShort(wav, 34, 16);
        WriteString(wav, 36, "data");
        WriteInt(wav, 40, byteCount);

        int offset = 44;
        foreach (float s in samples)
        {
            short val = (short)Mathf.Clamp(s * 32767f, short.MinValue, short.MaxValue);
            wav[offset++] = (byte)(val & 0xFF);
            wav[offset++] = (byte)((val >> 8) & 0xFF);
        }

        return wav;
    }

    private static void WriteString(byte[] b, int offset, string s)
    {
        foreach (char c in s) b[offset++] = (byte)c;
    }

    private static void WriteInt(byte[] b, int offset, int v)
    {
        b[offset] = (byte)(v & 0xFF);
        b[offset + 1] = (byte)((v >> 8) & 0xFF);
        b[offset + 2] = (byte)((v >> 16) & 0xFF);
        b[offset + 3] = (byte)((v >> 24) & 0xFF);
    }

    private static void WriteShort(byte[] b, int offset, short v)
    {
        b[offset] = (byte)(v & 0xFF);
        b[offset + 1] = (byte)((v >> 8) & 0xFF);
    }
}