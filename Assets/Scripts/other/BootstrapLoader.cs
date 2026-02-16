using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    [SerializeField] private string lobbySceneName = "LobbyScene";

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != lobbySceneName)
        {
            SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
        }
    }
}