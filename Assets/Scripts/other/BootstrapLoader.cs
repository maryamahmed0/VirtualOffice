using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    [SerializeField] private string lobbySceneName = "LobbyScene";
    [SerializeField] private GameObject loginCanvas;

    private void Start()
    {
        if (AuthBridge.IsReady)
        {
      
            Debug.Log("[Bootstrap] AuthBridge ready, going to Lobby");
            GoToLobby();
        }
        else
        {
           
            Debug.Log("[Bootstrap] Showing Login UI");
            if (loginCanvas != null)
                loginCanvas.SetActive(true);
        }
    }

    public void GoToLobby()
    {
        if (loginCanvas != null)
            loginCanvas.SetActive(false);
        SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
    }
}