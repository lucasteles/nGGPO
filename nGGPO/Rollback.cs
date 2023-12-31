﻿using nGGPO.Backends;
using nGGPO.Serialization;

namespace nGGPO;

public class Rollback
{
    public static IRollbackSession<TInput, TGameState> CreateSession<TInput, TGameState>(
        ISessionCallbacks<TGameState> cb,
        int numPlayers,
        int localPort,
        IBinarySerializer<TInput>? inputSerializer = null
    )
        where TInput : struct
        where TGameState : struct
    {
        inputSerializer ??= BinarySerializers.Get<TInput>()
                            ?? throw new InvalidOperationException(
                                $"Unable to infer serializer for type {typeof(TInput).FullName}");

        return new Peer2PeerBackend<TInput, TGameState>(inputSerializer, cb, localPort, numPlayers);
    }
}