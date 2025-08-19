using Fusion;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Unity.Collections.Unicode;
using static UnityEngine.GraphicsBuffer;

public class FusionBridge : NetworkBehaviour
{
    public static FusionBridge Instance;
    [Networked] public int PlayerId { get; set; }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            base.Spawned();
            Instance = this;
            Debug.Log("FusionBridge Spawned on network.");

            // Local player → set ID
            PlayerId = Runner.IsSharedModeMasterClient ? 1 : 2;
            UIManager.Instance.localPlayerId = PlayerId;

            Debug.Log($"FusionBridge Spawned → Local PlayerId = {PlayerId}");
        }
    }

    public void StartMultiplayerGame()
    {
        if (Runner.IsSharedModeMasterClient)
        {
            int seed = UnityEngine.Random.Range(0, 999999);
            RpcSendSeed(seed);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcSendSeed(int seed)
    {
        Debug.Log($"Seed received: {seed}");
        UIManager.Instance.StartMultiplayerWords(seed);
    }

    // FusionBridge.cs ma add karo
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcWordCompleted(int playerId, string word, bool isCorrect)
    {
        if (Runner.LocalPlayer.PlayerId != playerId)
            UIManager.Instance.OpponentWord(isCorrect);
    }
}
