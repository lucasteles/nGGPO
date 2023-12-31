﻿using System.Net;
using nGGPO.Input;
using nGGPO.Network;
using nGGPO.Network.Messages;
using nGGPO.Serialization;
using nGGPO.Utils;

namespace nGGPO.Backends;

class Peer2PeerBackend<TInput, TGameState> : IRollbackSession<TInput, TGameState>
    where TInput : struct
    where TGameState : struct
{
    const int RecommendationInterval = 240;
    const int DefaultDisconnectTimeout = 5000;
    const int DefaultDisconnectNotifyStart = 750;
    const int SpectatorOffset = 1000;

    readonly IBinarySerializer<TInput> inputSerializer;
    readonly ISessionCallbacks<TGameState> callbacks;

    readonly Udp udp;
    readonly Synchronizer<TGameState> sync;
    readonly ConnectStatus[] localConnectStatus = new ConnectStatus[Max.MsgPlayers];

    int numPlayers;
    int numSpectators;
    bool synchronizing = true;

    readonly List<UdpProtocol> spectators = new(Max.Spectators);
    readonly List<UdpProtocol> endpoints;

    int disconnectTimeout = DefaultDisconnectTimeout;
    int disconnectNotifyStart = DefaultDisconnectNotifyStart;

    public Peer2PeerBackend(
        IBinarySerializer<TInput> inputSerializer,
        ISessionCallbacks<TGameState> callbacks,
        int localPort,
        int numPlayers
    )
    {
        this.inputSerializer = inputSerializer;
        this.callbacks = callbacks;
        this.numPlayers = numPlayers;

        endpoints = new(numPlayers);
        sync = new(localConnectStatus);

        udp = new(localPort);
    }

    public ErrorCode AddPlayer(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (player is Player.Spectator spectator)
            return AddSpectator(spectator.EndPoint);

        if (player.PlayerNumber < 1 || player.PlayerNumber > numPlayers)
            return ErrorCode.PlayerOutOfRange;

        var queue = player.PlayerNumber - 1;
        player.SetHandle(QueueToPlayerHandle(queue));

        if (player is Player.Remote remote)
            AddRemotePlayer(remote.EndPoint, queue);

        return ErrorCode.Ok;
    }

    public ErrorCode SetFrameDelay(Player player, int delay)
    {
        var result = PlayerHandleToQueue(player.Handle, out var queue);

        if (result is not ErrorCode.Ok)
            return result;

        sync.SetFrameDelay(queue, delay);

        return ErrorCode.Ok;
    }

    GameInput ParseInput(ref TInput input)
    {
        GameInputBuffer buffer = new();
        var size = inputSerializer.Serialize(ref input, buffer);
        return new GameInput(ref buffer, size);
    }

    public async Task<ErrorCode> AddLocalInput(PlayerHandle player, TInput localInput)
    {
        if (sync.InRollback())
            return ErrorCode.InRollback;

        if (synchronizing)
            return ErrorCode.NotSynchronized;

        var result = PlayerHandleToQueue(player, out var queue);
        if (!result.IsSuccess())
            return result;

        var input = ParseInput(ref localInput);

        if (!sync.AddLocalInput(queue, input))
            return ErrorCode.PredictionThreshold;

        if (input.Frame.IsNull) return ErrorCode.Ok;

        Tracer.Log("setting local connect status for local queue {0} to {1}",
            queue, input.Frame);

        localConnectStatus[queue].LastFrame = input.Frame;

        // Send the input to all the remote players.
        for (var i = 0; i < numPlayers; i++)
            if (endpoints[i].IsInitialized())
                await endpoints[i].SendInput(in input);

        return ErrorCode.Ok;
    }

    public ErrorCode SynchronizeInputs(params TInput[] inputs) => SynchronizeInputs(out _, inputs);

    public ErrorCode SynchronizeInputs(out int[] disconnectFlags, params TInput[] inputs)
    {
        if (synchronizing)
        {
            disconnectFlags = Array.Empty<int>();
            return ErrorCode.NotSynchronized;
        }

        disconnectFlags = sync.SynchronizeInputs(inputs);
        return ErrorCode.Ok;
    }

    ErrorCode PlayerHandleToQueue(PlayerHandle player, out int queue)
    {
        var offset = player.Value - 1;
        if (offset < 0 || offset >= numPlayers)
        {
            queue = PlayerHandle.Empty.Value;
            return ErrorCode.InvalidPlayerHandle;
        }

        queue = offset;
        return ErrorCode.Ok;
    }

    protected PlayerHandle QueueToPlayerHandle(int queue) => new(queue + 1);

    protected PlayerHandle QueueToSpectatorHandle(int queue) =>
        new(queue + SpectatorOffset); /* out of range of the player array, basically */

    UdpProtocol CreateUdpProtocol(IPEndPoint endpoint, int queue)
    {
        UdpProtocol protocol = new(
            timesync: new(),
            random: Rnd.Shared,
            udp: udp,
            queue,
            endpoint,
            localConnectStatus
        )
        {
            DisconnectTimeout = disconnectTimeout,
            DisconnectNotifyStart = disconnectNotifyStart,
        };

        protocol.Synchronize();
        return protocol;
    }

    void AddRemotePlayer(IPEndPoint endpoint, int queue)
    {
        /*
         * Start the state machine (xxx: no)
         */
        synchronizing = true;

        var protocol = CreateUdpProtocol(endpoint, queue);
        endpoints.Add(protocol);
    }

    ErrorCode AddSpectator(IPEndPoint endpoint)
    {
        if (numSpectators == Max.Spectators)
            return ErrorCode.TooManySpectators;

        /*
         * Currently, we can only add spectators before the game starts.
         */
        if (!synchronizing)
            return ErrorCode.InvalidRequest;

        var queue = numSpectators++;
        var protocol = CreateUdpProtocol(endpoint, queue + SpectatorOffset);
        spectators.Add(protocol);

        return ErrorCode.Ok;
    }

    public void Dispose() => udp.Dispose();
}
