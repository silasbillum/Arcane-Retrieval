using UnityEngine;
using Unity.Netcode;

public class NetworkPersist : MonoBehaviour
{
    void Awake()
    {
        if (FindObjectsOfType<NetworkManager>().Length > 1)
        {
            Destroy(gameObject); // prevent duplicates
            return;
        }

        DontDestroyOnLoad(gameObject); // keep across scenes
    }
}
