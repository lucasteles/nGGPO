﻿using System.Runtime.InteropServices;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

[StructLayout(LayoutKind.Sequential)]
struct InputAck
{
    public int AckFrame;

    public void Serialize(NetworkBufferWriter writer) => writer.Write(AckFrame);

    public void Deserialize(NetworkBufferReader reader) =>
        AckFrame = reader.ReadInt();
}