using Fusion;
using UnityEngine;

public struct PlayerInputData : INetworkInput
{
    public Vector2 move;
    public Vector2 look;
    public NetworkBool run;
    public NetworkBool crouch;
    public NetworkBool jump;
    public NetworkBool slide;
    public NetworkBool toggleCursor;
}
