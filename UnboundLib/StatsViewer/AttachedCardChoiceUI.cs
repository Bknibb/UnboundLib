using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace UnboundLib.StatsViewer
{
    class AttachedCardChoiceUI : MonoBehaviour
    {
        public static AttachedCardChoiceUI instance;
        private TextMeshProUGUI playerNameText;
        public void Awake()
        {
            instance = this;
            StatsViewer.Init();
            GameObject playerName = new GameObject("PlayerName");
            playerName.transform.SetParent(this.transform);
            RectTransform playerNameRectTransform = playerName.AddComponent<RectTransform>();
            playerNameRectTransform.anchorMin = new Vector2(0.5f, 1);
            playerNameRectTransform.anchorMax = new Vector2(0.5f, 1);
            playerNameRectTransform.pivot = new Vector2(0.5f, 1);
            playerNameRectTransform.sizeDelta = new Vector2(500, 100);
            playerNameRectTransform.localScale = Vector3.one;
            playerNameText = playerName.AddComponent<TextMeshProUGUI>();
            playerNameText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            playerNameText.enableAutoSizing = true;
            playerNameText.fontSizeMax = 100;
        }
        public void Update()
        {
            StatsViewer.UpdateControls();
        }
        public void ChangePlayer(Player newPlayer)
        {
            StatsViewer.ChangePlayer(newPlayer);
            playerNameText.text = string.Empty;
            if (!PhotonNetwork.OfflineMode)
            {
                playerNameText.text = newPlayer.data.view.Owner.NickName;
            }
        }
    }
}
