using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RasonNetwork;

public class M_menu : MonoBehaviour
{

    public GameObject Player;
    public Transform SpawnPoint;

    public GameObject MenuUI;
    public GameObject DisconnButtom;
    public InputField ConnIP;

    void Start()
    {
        MenuUI.SetActive(true);
        DisconnButtom.SetActive(false);

        ConnIP.text = "127.0.0.1";
    }

    void OnEnable()
    {
        RasonManager.onJoinRoom += OnJoinedGame;
        RasonManager.onDisconnect += OnDisconnected;
        RasonManager.onPlayerJoin += OnPlayerJoin;
        RasonManager.onPlayerLeft += OnPlayerLeft;
    }
    void OnDisable()
    {
        RasonManager.onJoinRoom -= OnJoinedGame;
        RasonManager.onDisconnect -= OnDisconnected;
        RasonManager.onPlayerJoin -= OnPlayerJoin;
        RasonManager.onPlayerLeft -= OnPlayerLeft;
    }
    void OnPlayerJoin(RPlayer p)
    {
        Debug.Log("New player connected '" + p.name + "'");
    }
    void OnPlayerLeft(RPlayer p)
    {
        Debug.Log("Player disconnected '" + p.name + "'");
    }

    /// <summary>
    /// Called when the game server started or connected to game server.
    /// </summary>

    void OnJoinedGame(bool succes, int rid)
    {
        MenuUI.SetActive(false);
        DisconnButtom.SetActive(true);
        RasonManager.Create(Player, false, SpawnPoint.position, SpawnPoint.rotation);
    }

    void OnDisconnected(bool byPlayer, LiteNetLib.DisconnectInfo info)
    {
        MenuUI.SetActive(true);
        DisconnButtom.SetActive(false);
    }

    public void StartServer()
    {
        ServerInstance.StartServer(2151, 8);
    }

    public void OnClickConnect()
    {
        RasonManager.Connect(ConnIP.text, 2151);
    }

    public void OnClickDisconnect()
    {
        RasonManager.Disconnect();
    }

    public void OnClickFindOnLAN()
    {
        // Send a descovery request.
        RasonManager.SendServerDiscovery(2151);

        // Check the available LAN servers
        ServerInfo[] lanServers = RasonManager.GetAvailableLanServers();

        if (lanServers.Length != 0)
        {
            // Peck up the first one.
            ConnIP.text = lanServers[0].ip;
        }
    }
}