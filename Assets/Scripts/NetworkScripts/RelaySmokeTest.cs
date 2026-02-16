using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Core.Environments;

public class RelaySmokeTest : MonoBehaviour
{
    [SerializeField] string environmentName = "production"; // جرّبي كمان "default" لو عندك Environment اسمها default
    private bool inited;

    private async Task InitOnce()
    {
        if (inited) return;

        var options = new InitializationOptions().SetEnvironmentName(environmentName);
        await UnityServices.InitializeAsync(options);

        AuthenticationService.Instance.ClearSessionToken();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        Debug.Log($"[SMOKE] Inited Env={environmentName} ProjectId={Application.cloudProjectId} PlayerId={AuthenticationService.Instance.PlayerId}");
        inited = true;
    }

    public async void CreateCode()
    {
        try
        {
            await InitOnce();
            Allocation a = await RelayService.Instance.CreateAllocationAsync(2);
            string code = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);
            code = code.Trim().ToUpperInvariant();

            Debug.Log($"[SMOKE][HOST] AllocationId={a.AllocationId} Region={a.Region} Code={code}");
            GUIUtility.systemCopyBuffer = code;
            Debug.Log("[SMOKE][HOST] Code copied to clipboard");
        }
        catch (Exception e)
        {
            Debug.LogError("[SMOKE][HOST] FAILED: " + e);
        }
    }

    public async void JoinWithClipboard()
    {
        try
        {
            await InitOnce();
            string code = GUIUtility.systemCopyBuffer.Trim().ToUpperInvariant();
            Debug.Log($"[SMOKE][CLIENT] Joining Code={code}");

            var j = await RelayService.Instance.JoinAllocationAsync(code);
            Debug.Log($"[SMOKE][CLIENT] SUCCESS AllocationId={j.AllocationId} Region={j.Region}");
        }
        catch (Exception e)
        {
            Debug.LogError("[SMOKE][CLIENT] FAILED: " + e);
        }
    }
}