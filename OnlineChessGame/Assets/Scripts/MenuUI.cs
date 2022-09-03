using System;
using TMPro;
using UnityEngine;

public enum CameraAngle
{
    menu = 0,
    whiteTeam = 1,
    blackTeam = 2
}

public class MenuUI : MonoBehaviour
{
    public static MenuUI Instance { get; set; }

    public Server server;
    public Client client;

    [SerializeField] private TMP_InputField addressInput;
    [SerializeField] private Animator menuAnimator;
    [SerializeField] private GameObject[] cameraAngles;

    public Action<bool> SetLocalGame;

    private void Awake()
    {
        Instance = this;

        RegisterEvents();
    }

    // Cameras
    public void ChangeCamera(CameraAngle index)
    {
        for (int i = 0; i < cameraAngles.Length; i++)
            cameraAngles[i].SetActive(false);

        cameraAngles[(int)index].SetActive(true);
    }

    // Buttons
    public void OnLocalGameButton()
    {
        menuAnimator.SetTrigger("InGame");
        server.Init(8008);
        client.Init("127.0.0.1", 8008);
        SetLocalGame?.Invoke(true);
    }
    public void OnOnlineGameButton()
    {
        menuAnimator.SetTrigger("OnlineGameMenu");
    }
    public void OnHostButton()
    {
        server.Init(8008);
        client.Init("127.0.0.1", 8008);
        menuAnimator.SetTrigger("HostMenu");
        SetLocalGame?.Invoke(false);
    }
    public void OnConnectButton()
    {
        client.Init(addressInput.text, 8008);
        SetLocalGame?.Invoke(false);
    }
    public void OnBackToMainMenuButton()
    {
        menuAnimator.SetTrigger("MainMenu");
    }
    public void OnBackToOnlineGameMenuButton()
    {
        server.Shutdown();
        client.Shutdown();
        menuAnimator.SetTrigger("OnlineGameMenu");
    }


    private void RegisterEvents()
    {
        NetUtility.C_START_GAME += OnStartGameClient;
    }

    private void UnRegisterEvents()
    {
        NetUtility.C_START_GAME -= OnStartGameClient;
    }


    private void OnStartGameClient(NetMessage obj)
    {
        menuAnimator.SetTrigger("InGame");
    }

}
