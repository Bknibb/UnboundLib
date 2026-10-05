using HarmonyLib;
using InControl;
using UnboundLib.Cards;
using UnityEngine;

namespace UnboundLib.Patches
{

    [HarmonyPatch(typeof(CardBarHandler), "AddCard")]
    class CardBarHandler_Patch_AddCard
    {
        static void Prefix(int teamId, CardInfo card)
        {
            CardData.AddCard(teamId, card.CardName);
        }
    }

    [HarmonyPatch(typeof(CardBarHandler), "Update")]
    class CardBarHandler_Patch_Update
    {
        static void Prefix(CardBar[] ___cardBars)
        {
            foreach (InputDevice inputDevice in InputManager.ActiveDevices)
            {
                if (inputDevice.Action3.WasPressed)
                {
                    bool newState = !___cardBars[0].gameObject.activeSelf;
                    foreach (CardBar cardBar in ___cardBars)
                    {
                        cardBar.gameObject.SetActive(newState);
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.Tab) && !DevConsole.isTyping)
            {
                bool newState = !___cardBars[0].gameObject.activeSelf;
                foreach (CardBar cardBar in ___cardBars)
                {
                    cardBar.gameObject.SetActive(newState);
                }
            }
        }
    }
}
