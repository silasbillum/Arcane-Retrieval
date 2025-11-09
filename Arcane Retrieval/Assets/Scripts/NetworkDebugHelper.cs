using Fusion;
using UnityEngine;

public class NetworkDebugHelper : NetworkBehaviour
{
    public override void Spawned()
    {
        Debug.Log($"[Spawned] {name} | Owner={Object.InputAuthority} | HasInputAuthority={Object.HasInputAuthority}");
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasInputAuthority)
        {
            Debug.DrawRay(transform.position + Vector3.up * 2, Vector3.up * 2, Color.green);
        }
        else
        {
            Debug.DrawRay(transform.position + Vector3.up * 2, Vector3.up * 2, Color.red);
        }
    }
}
