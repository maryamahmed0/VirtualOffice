using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Core.Environments;

public class UGSBootstrap : MonoBehaviour
{
    private static Task initTask;
    private static string usedProfile;

    public static async Task EnsureSignedIn()
    {
        string profile = Application.isEditor ? "editor" : "build";

        if (initTask != null && usedProfile != profile)
        {
            Debug.LogWarning($"[UGS] Already initialized with profile '{usedProfile}'. Restart app to use '{profile}'.");
            return;
        }

        usedProfile = profile;

        if (initTask == null)
            initTask = Init(profile);

        await initTask;
    }

    private static async Task Init(string profile)
    {
        const string envName = "production"; 

        var options = new InitializationOptions()
            .SetEnvironmentName(envName)
            .SetProfile(profile);

        await UnityServices.InitializeAsync(options);

        Debug.Log($"[UGS] cloudProjectId={Application.cloudProjectId}");
        Debug.Log($"[UGS] env={envName}");
        Debug.Log($"[UGS] profile={profile}");

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        Debug.Log($"[UGS] Signed in. PlayerId={AuthenticationService.Instance.PlayerId}");
    }
}