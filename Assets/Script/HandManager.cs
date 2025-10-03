using UnityEngine;
using Oculus.Interaction.Input;

public class HandManager : MonoBehaviour
{
    public static HandManager instance { get; private set; }
    public Hand leftHand;
    public Hand rightHand;

    void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }
}
