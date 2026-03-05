using System;
using Unity.Collections;
using Unity.Netcode;

[Serializable]
public struct PresenceEntry : INetworkSerializable
{
    public ulong ClientId;
    public FixedString64Bytes Name;
    public FixedString32Bytes Org;
    public int TeamIdHash;
    public int Zone; // cast from NetRoomState.Zone

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref Org);
        serializer.SerializeValue(ref TeamIdHash);
        serializer.SerializeValue(ref Zone);
    }
}