﻿using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using nGGPO.Inputs;
using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;
using nGGPO.Utils;

byte[] data = {1, 2, 3, 4, 5};

{
    var packet = long.MaxValue;
    var serializer = BinarySerializers.Get<long>()!;
    using var buffer = MemoryBuffer.Rent(10, true);
    var size = serializer.Serialize(in packet, buffer);
    var bytes = buffer.Span[..size];

    Console.WriteLine($"# Size={size}\n");
    var backPacket = serializer.Deserialize(bytes);
    Console.WriteLine($"# Pkg={backPacket}\n");
}

{
    var packet = ButtonsInput.UpLeft | ButtonsInput.X;
    var serializer = BinarySerializers.Get<ButtonsInput>()!;
    using var buffer = MemoryBuffer.Rent(10, true);
    var size = serializer.Serialize(in packet, buffer);
    var bytes = buffer.Span[..size];
    Console.WriteLine($"# Size={size}\n");
    var backPacket = serializer.Deserialize(bytes);
    var buttons = new ButtonsInputEditor(backPacket);
    Console.WriteLine($"# Pkg= {buttons}\n");
}


{
    Input packet = new()
    {
        S = data.Length,
        A = (byte) 'a',
        B = 2,
        Bits = new(),
    };
    data.CopyTo(packet.Bits);
    Console.WriteLine($"# Ipt: {packet}\n");


    var serializer = BinarySerializers.Get<Input>()!;
    using var buffer = MemoryBuffer.Rent(20, true);
    var size = serializer.Serialize(in packet, buffer);
    var bytes = buffer.Span[..size];

    Console.WriteLine($"# Size={size}\n");
    var backPacket = serializer.Deserialize(bytes);
    Console.WriteLine($"# Pkg: {backPacket}\n");
}


{
    Input packet = new()
    {
        S = data.Length,
        A = (byte) 'a',
        B = 2,
        Bits = new(),
    };
    data.CopyTo(packet.Bits);

    // var serializer = new CustomInputSerializer();
    //
    // using var buffer = MemoryBuffer.Rent(20, true);
    // var size = serializer.Serialize(in packet, buffer);
    // var bytes = buffer.Span[..size];
    //
    // Console.WriteLine($"# Size={size}\n");
    // var backPacket = serializer.Deserialize(bytes);
    // Console.WriteLine($"# Pkg={backPacket}\n");
}

// Console.WriteLine($"# Size={size}, SizeM={sizeM}\n");

// using var bufferMarshal = Mem.StructToBytes(packet);

// serializer.Network = false;
// using var buffer = serializer.Serialize(packet);
//
// serializer.Network = true;
// using var bufferNetWork = serializer.Serialize(packet);
//
// Dump(bufferMarshal, "Marshall");
// Dump(buffer, "Serial");
// Dump(bufferNetWork, "Network");
//
// var valueMarshall = Mem.BytesToStruct<Input>(bufferMarshal);
// serializer.Network = false;
// var value = serializer.Deserialize(buffer);
// serializer.Network = true;
// var valueNetwork = serializer.Deserialize(bufferNetWork);
//
// Console.WriteLine();
// Console.WriteLine(valueMarshall);
// Console.WriteLine(value);
// Console.WriteLine(valueNetwork);

[InlineArray(10)]
[DebuggerDisplay("Buffer {ToString()}")]
public struct ValueBuffer
{
#pragma warning disable CS0169 // Field is never used
    byte element0;
#pragma warning restore CS0169 // Field is never used

    public override string ToString()
    {
        ReadOnlySpan<byte> bytes = this;
        return $"[{string.Join(", ", bytes.ToArray().Select(x => (int) x))}]";
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Input
{
    public int S;
    public byte A;
    public uint B;

    [JsonIgnore]
    public ValueBuffer Bits;

    // [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
    // public byte[] Bits; /* must be last */

    public override string ToString() =>
        $"{JsonSerializer.Serialize(this, new JsonSerializerOptions {IncludeFields = true})}; Buffer: {Bits}";
}

class CustomInputSerializer : BinarySerializer<Input>
{
    protected override void Serialize(ref NetworkBufferWriter writer, in Input data)
    {
        writer.Write(data.S);
        writer.Write(data.A);
        writer.Write(data.B);

        var bits = data.Bits[..data.S];
        bits.CopyTo(writer.CurrentBuffer);
        writer.Advance(bits.Length);
        // writer.WriteBytes(bits);
    }

    protected override Input Deserialize(ref NetworkBufferReader reader)
    {
        var size = reader.ReadInt();
        var input = new Input
        {
            S = size,
            A = reader.ReadByte(),
            B = reader.ReadUInt(),
            // Bits = new byte[size],
        };
        // reader.ReadByte(input.Bits);
        return input;
    }
}