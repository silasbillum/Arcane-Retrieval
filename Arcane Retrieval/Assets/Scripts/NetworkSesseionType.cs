using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkSessionType : MonoBehaviour
{
    public static bool IsHost;

    public void OnHostClicked()
    {
        IsHost = true;
        SceneManager.LoadScene("LobbyScene");
    }

    public void OnJoinClicked()
    {
        IsHost = false;
        SceneManager.LoadScene("LobbyScene");
    }
}
