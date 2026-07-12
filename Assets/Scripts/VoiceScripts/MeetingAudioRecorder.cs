using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class MeetingAudioRecorder : NetworkBehaviour
{
    [Header("Recording Settings")]
    [SerializeField] private int sampleRate = 16000;

   
    [SerializeField] private float chunkSeconds = 2f;

    [SerializeField] private int maxRecordSeconds = 120;

    [Header("State (Debug)")]
    [SerializeField] private bool isRecording;
    [SerializeField] private string currentRoomId;
    [SerializeField] private int chunksSent;

    private AudioClip _micClip;
    private string _micDevice;
    private int _lastSamplePos;
    private Coroutine _sendLoop;
    private NetRoomState _localRoom;

    private void OnEnable()
    {
        StartCoroutine(HookLocalRoom());
    }

    private void OnDisable()
    {
        StopRecording();

        if (_localRoom != null)
            _localRoom.CurrentZone.OnValueChanged -= OnZoneChanged;
    }

    private IEnumerator HookLocalRoom()
    {
        Debug.Log("[AUDIO REC] Waiting for PlayerRoomState...");

        while (PlayerRoomState.LocalInstance == null)
            yield return null;

        _localRoom = PlayerRoomState.LocalInstance.GetComponentInParent<NetRoomState>();

        if (_localRoom == null)
        {
            Debug.LogError("[AUDIO REC] NetRoomState NOT FOUND!");
            yield break;
        }

        _localRoom.CurrentZone.OnValueChanged -= OnZoneChanged;
        _localRoom.CurrentZone.OnValueChanged += OnZoneChanged;

        OnZoneChanged(_localRoom.CurrentZone.Value, _localRoom.CurrentZone.Value);
    }

    private void OnZoneChanged(int oldZ, int newZ)
    {
        var zone = (NetRoomState.Zone)newZ;

        if (zone == NetRoomState.Zone.Meeting)
            StartRecording();
        else
            StopRecording();
    }

    public void StartRecording()
    {
        if (isRecording) return;
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[AUDIO REC] No microphone found!");
            return;
        }

        _micDevice = Microphone.devices[0];
        _micClip = Microphone.Start(_micDevice, true, maxRecordSeconds, sampleRate);
        _lastSamplePos = 0;
        chunksSent = 0;

        currentRoomId = GameSessionData.Instance != null
            ? GameSessionData.Instance.LastJoinCode
            : "unknown";

        isRecording = true;

      
        if (IsServer)
            MeetingAudioMixer.Instance?.StartSession(currentRoomId);

        if (_sendLoop != null) StopCoroutine(_sendLoop);
        _sendLoop = StartCoroutine(SendChunksLoop());

        Debug.Log($"[AUDIO REC] Recording started. RoomId={currentRoomId} mic={_micDevice}");
    }

    public void StopRecording()
    {
        if (!isRecording) return;
        isRecording = false;

        if (_sendLoop != null)
        {
            StopCoroutine(_sendLoop);
            _sendLoop = null;
        }

        SendRemainingAudio();

        if (_micDevice != null && Microphone.IsRecording(_micDevice))
            Microphone.End(_micDevice);

        _micClip = null;

        if (IsServer)
            MeetingAudioMixer.Instance?.EndSession(currentRoomId);

        Debug.Log($"[AUDIO REC] Recording stopped. Total chunks sent={chunksSent}");
    }



    private IEnumerator SendChunksLoop()
    {
        var wait = new WaitForSeconds(chunkSeconds);

        while (isRecording)
        {
            yield return wait;
            if (!isRecording) break;
            ExtractAndSendChunk(isLastChunk: false);
        }
    }

    private void SendRemainingAudio()
    {
        ExtractAndSendChunk(isLastChunk: true);
    }

    private void ExtractAndSendChunk(bool isLastChunk)
    {
        if (_micClip == null) return;

        int currentPos = Microphone.GetPosition(_micDevice);
        Debug.Log($"[AUDIO REC] Mic Position = {currentPos} | Last Position = {_lastSamplePos}");

        if (currentPos < 0) return;
        if (!isLastChunk && currentPos == _lastSamplePos) return;

        int sampleCount = currentPos >= _lastSamplePos
            ? currentPos - _lastSamplePos
            : (_micClip.samples - _lastSamplePos) + currentPos;

        if (sampleCount <= 0) return;

        float[] samples = new float[sampleCount];
        _micClip.GetData(samples, _lastSamplePos % _micClip.samples);
        _lastSamplePos = currentPos;

        byte[] pcm16 = FloatsToPcm16Bytes(samples);

        Debug.Log($"[AUDIO REC] Sending Chunk {chunksSent} | Sample Count={sampleCount} | Bytes={pcm16.Length} | isLast={isLastChunk}");
        SendAudioChunkServerRpc(pcm16, chunksSent, isLastChunk);
        chunksSent++;
    }

    [ServerRpc]
    private void SendAudioChunkServerRpc(byte[] pcm16, int chunkIndex, bool isLastChunk, ServerRpcParams rpcParams = default)
    {
        ulong speakerClientId = rpcParams.Receive.SenderClientId;

        if (MeetingAudioMixer.Instance == null)
        {
            Debug.LogError("[AUDIO REC] MeetingAudioMixer.Instance is NULL on the host! " +
                            "Add the MeetingAudioMixer component to a GameObject in the scene, or this chunk is silently dropped.");
            return;
        }

        Debug.Log($"[AUDIO REC][HOST] Chunk {chunkIndex} received from client {speakerClientId} isLast={isLastChunk}");
        MeetingAudioMixer.Instance.AddChunk(currentRoomId, speakerClientId.ToString(), chunkIndex, pcm16, isLastChunk);
    }

    private static byte[] FloatsToPcm16Bytes(float[] samples)
    {
        byte[] bytes = new byte[samples.Length * 2];
        int offset = 0;
        foreach (float s in samples)
        {
            short val = (short)Mathf.Clamp(s * 32767f, short.MinValue, short.MaxValue);
            bytes[offset++] = (byte)(val & 0xFF);
            bytes[offset++] = (byte)((val >> 8) & 0xFF);
        }
        return bytes;
    }
}