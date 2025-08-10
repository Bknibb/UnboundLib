using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnboundLib.Utils.UI;
using UnityEngine;

namespace UnboundLib.Networking
{
    public class KickMenu : MonoBehaviourPunCallbacks
    {
        public static KickMenu instance;
        static KickMenu() {
            PhotonNetwork.EnableCloseConnection = true;
        }
        internal static void Init(bool firstTime)
        {
            Unbound.Instance.ExecuteAfterSeconds(firstTime ? 0.1f : 0, () =>
            {
                InitUI();
            });
        }
        private static void InitUI()
        {
            GameObject menu = MenuHandler.CreateMenu("KICK MENU", () => { }, UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main").gameObject, 60, true, false, UIHandler.instance.transform.Find("Canvas/EscapeMenu").gameObject, true, 3);
            menu.AddComponent<KickMenu>();
            UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Group").Find("KICK MENU").gameObject.SetActive(false);
        }
        public void Awake()
        {
            instance = this;
        }
        public void Refresh()
        {
            foreach (Transform child in transform.Find("Group/Grid/Scroll View/Viewport/Content"))
            {
                Destroy(child.gameObject);
            }
            foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                if (player.IsLocal) continue;
                var button = MenuHandler.CreateButton(player.NickName, gameObject, () => KickPlayer(player));
            }
            gameObject.GetComponent<ListMenuPage>().SetFieldValue("firstSelected", transform.GetChild(0).GetComponentInChildren<ListMenuButton>());
        }
        private IEnumerator RefreshAfterNameResolveCoroutine()
        {
            yield return new WaitUntil(() => PhotonNetwork.CurrentRoom.Players.Values.All(p => !string.IsNullOrEmpty(p.NickName) && p.NickName != "PlayerName"));
            Refresh();
        }
        public void RefreshAfterNameResolve()
        {
            if (PhotonNetwork.OfflineMode) return;
            StartCoroutine(RefreshAfterNameResolveCoroutine());
        }
        public void KickPlayer(Photon.Realtime.Player player)
        {
            StartCoroutine(KickPlayerCoroutine(player));
        }
        private IEnumerator KickPlayerCoroutine(Photon.Realtime.Player player)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC_Others(typeof(KickMenu), nameof(RPC_KickPlayer), player.ActorNumber);
                yield return new WaitForSeconds(0.1f);
                PhotonNetwork.CloseConnection(player);
            }
            else
            {
                Debug.LogWarning("You must be the master client to kick players.");
            }
        }
        [UnboundRPC]
        public static void RPC_KickPlayer(int actorNumber)
        {
            if (!actorNumber.Equals(PhotonNetwork.LocalPlayer.ActorNumber)) return;
            PhotonNetwork.NetworkingClient.Disconnect(DisconnectCause.DisconnectByServerLogic);
        }
        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            if (PhotonNetwork.OfflineMode || !PhotonNetwork.IsMasterClient) return;
            Refresh();
        }
        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            if (PhotonNetwork.OfflineMode || !PhotonNetwork.IsMasterClient) return;
            RefreshAfterNameResolve();
        }
        public override void OnJoinedRoom()
        {
            if (PhotonNetwork.OfflineMode || !PhotonNetwork.IsMasterClient) return;
            UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Group/KICK MENU").gameObject.SetActive(true);
            Refresh();
        }
        public override void OnLeftRoom()
        {
            if (PhotonNetwork.OfflineMode) return;
            UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Group/KICK MENU").gameObject.SetActive(false);
            foreach (Transform child in transform.Find("Group/Grid/Scroll View/Viewport/Content"))
            {
                Destroy(child.gameObject);
            }
        }
    }
}
