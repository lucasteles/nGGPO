﻿using System;
using System.Threading.Tasks;
using nGGPO.Types;

namespace nGGPO;

public interface ISession<TInput, TGameState> : IDisposable
    where TInput : struct
    where TGameState : struct
{
    public ErrorCode AddPlayer(Player player);
    public ErrorCode SetFrameDelay(Player player, int frameDelay);
    public Task<ErrorCode> AddLocalInput(PlayerHandle player, TInput input);
    public ErrorCode SynchronizeInputs(params TInput[] inputs);
    public ErrorCode SynchronizeInputs(out int[] disconnectFlags, params TInput[] inputs);
}