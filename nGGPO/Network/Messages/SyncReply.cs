﻿using nGGPO.Serialization;

namespace nGGPO.Network.Messages;

struct SyncReply
{
    public uint RandomReply; /* please reply back with this random data */

    public const int Size = sizeof(uint);

    public class Serializer : BinarySerializer<SyncReply>
    {
        public static readonly Serializer Instance = new();

        public override int SizeOf(in SyncReply data) => Size;

        protected internal override void Serialize(
            ref NetworkBufferWriter writer,
            in SyncReply data)
        {
            writer.Write(data.RandomReply);
        }

        protected internal override SyncReply Deserialize(ref NetworkBufferReader reader) =>
            new()
            {
                RandomReply = reader.ReadUInt(),
            };
    }
}