﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Utils;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using TMPro;
using UnboundLib.GameModes;
using UnboundLib.Networking;
using UnboundLib.Utils;
using UnboundLib.Utils.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static UnboundLib.Utils.UI.UpdateChecker;

namespace UnboundLib
{
    [BepInPlugin(ModId, ModName, Version)]
    [BepInProcess("Rounds.exe")]
    public class Unbound : BaseUnityPlugin
    {
        private const string ModId = "com.willis.rounds.unbound";
        private const string ModName = "Rounds Unbound";
        public const string Version = "4.0.6";

        public static Unbound Instance { get; private set; }
        public static readonly ConfigFile config = new ConfigFile(Path.Combine(Paths.ConfigPath, "UnboundLib.cfg"), true);
        public static readonly ConfigEntry<bool> lockMouse = BindConfig("Config Options", "LockMouse", true, new ConfigDescription("Lock mouse to the game window (normally the game only does this for exclusive fullscreen)"));

        private Canvas _canvas;
        public Canvas canvas
        {
            get
            {
                if (_canvas != null) return _canvas;
                _canvas = new GameObject("UnboundLib Canvas").AddComponent<Canvas>();
                _canvas.gameObject.AddComponent<GraphicRaycaster>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.pixelPerfect = false;
                DontDestroyOnLoad(_canvas);
                return _canvas;
            }
        }

        private struct NetworkEventType
        {
            public const string
                StartHandshake = "ModLoader_HandshakeStart",
                FinishHandshake = "ModLoader_HandshakeFinish";
        }

        internal static CardInfo templateCard;

        public delegate void OnJoinedDelegate();
        public delegate void OnLeftDelegate();
        public static event OnJoinedDelegate OnJoinedRoom;
        public static event OnLeftDelegate OnLeftRoom;

        internal static List<string> loadedGUIDs = new List<string>();
        internal static List<string> loadedMods = new List<string>();
        internal static List<string> loadedVersions = new List<string>();

        internal static List<Action> handShakeActions = new List<Action>();

        public static readonly Dictionary<string, bool> lockInputBools = new Dictionary<string, bool>();

        internal static AssetBundle UIAssets;
        public static AssetBundle toggleUI;
        internal static AssetBundle linkAssets;
        private static GameObject modalPrefab;

        private TextMeshProUGUI text;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int VK_LSHIFT = 0xA0;

        public static bool Loaded { get; private set; } = false;

        public Unbound()
        {
            if ((GetAsyncKeyState(VK_LSHIFT) & 0x8000) != 0)
            {
                Debug.LogWarning("UnboundLib was not loaded due to Left Shift being pressed!");
                DestroyImmediate(this);
                return;
            }
            Loaded = true;
            // Add UNBOUND text to the main menu screen
            bool firstTime = true;

            On.MainMenuHandler.Awake += (orig, self) =>
            {
                // reapply cards and levels
                this.ExecuteAfterFrames(5, () =>
                {
                    MapManager.instance.levels = LevelManager.activeLevels.ToArray();
                    CardManager.RestoreCardToggles();
                    ToggleCardsMenuHandler.RestoreCardToggleVisuals();

                });

                // create unbound text
                StartCoroutine(AddTextWhenReady(firstTime ? 2f : 0.1f));

                ModOptions.instance.CreateModOptions(firstTime);
                Credits.Instance.CreateCreditsMenu(firstTime);
                MainMenuLinks.AddLinks(firstTime);
                RegisterUpdateChecker("UnboundLib", Version, "Bknibb", "UnboundLib");

                var time = firstTime;
                this.ExecuteAfterSeconds(firstTime ? 0.4f : 0, () =>
                {
                    if (time)
                    {
                        CardManager.FirstTimeStart();
                    }
                });

                firstTime = false;

                orig(self);
            };

            On.MainMenuHandler.Close += (orig, self) =>
            {
                if (text != null) Destroy(text.gameObject);

                orig(self);
            };

            IEnumerator ArmsRaceStartCoroutine(On.GM_ArmsRace.orig_OnEnable orig, GM_ArmsRace self)
            {
                yield return GameModeManager.TriggerHook(GameModeHooks.HookInitStart);
                orig(self);
                yield return GameModeManager.TriggerHook(GameModeHooks.HookInitEnd);
            }

            On.GM_ArmsRace.OnEnable += (orig, self) =>
            {
                self.StartCoroutine(ArmsRaceStartCoroutine(orig, self));
            };

            IEnumerator SandboxStartCoroutine(On.GM_Test.orig_OnEnable orig, GM_Test self)
            {
                yield return GameModeManager.TriggerHook(GameModeHooks.HookInitStart);
                yield return GameModeManager.TriggerHook(GameModeHooks.HookInitEnd);
                yield return GameModeManager.TriggerHook(GameModeHooks.HookGameStart);
                orig(self);
                yield return GameModeManager.TriggerHook(GameModeHooks.HookRoundStart);
                yield return GameModeManager.TriggerHook(GameModeHooks.HookBattleStart);
            }

            On.GM_Test.OnEnable += (orig, self) =>
            {
                self.StartCoroutine(SandboxStartCoroutine(orig, self));
            };

            GameModeManager.AddHook(GameModeHooks.HookGameStart, handler => SyncModClients.DisableSyncModUi(SyncModClients.uiParent));

            // hook for closing ongoing lobbies
            GameModeManager.AddHook(GameModeHooks.HookGameStart, CloseLobby);

            On.CardChoice.Start += (orig, self) =>
            {
                for (int i = 0; i < self.cards.Length; i++)
                {
                    if (!((DefaultPool) PhotonNetwork.PrefabPool).ResourceCache.ContainsKey(self.cards[i].gameObject.name))
                        PhotonNetwork.PrefabPool.RegisterPrefab(self.cards[i].gameObject.name, self.cards[i].gameObject);
                }
                var children = new Transform[self.transform.childCount];
                for (int j = 0; j < children.Length; j++)
                {
                    children[j] = self.transform.GetChild(j);
                }
                self.SetFieldValue("children", children);
                self.cards = CardManager.activeCards.ToArray();
            };
        }

        private static IEnumerator CloseLobby(IGameModeHandler gm)
        {
            if (!PhotonNetwork.IsMasterClient || PhotonNetwork.OfflineMode) yield break;
            PhotonNetwork.CurrentRoom.IsVisible = false;
            PhotonNetwork.CurrentRoom.IsOpen = false;
            yield break;
        }

        private IEnumerator AddTextWhenReady(float delay = 0f, float maxTimeToWait = 10f)
        {
            if (delay > 0f) { yield return new WaitForSecondsRealtime(delay); }

            float time = maxTimeToWait;
            while (time > 0f && MainMenuHandler.instance?.transform?.Find("Canvas/ListSelector/Main/Group") == null)
            {
                time -= Time.deltaTime;
                yield return null;
            }
            if (MainMenuHandler.instance?.transform?.Find("Canvas/ListSelector/Main/Group") == null)
            {
                yield break;
            }
            text = MenuHandler.CreateTextAt("UNBOUND", Vector2.zero);
            text.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            text.fontSize = 30;
            text.color = (Color.yellow + Color.red) / 2;
            text.transform.SetParent(MainMenuHandler.instance.transform.Find("Canvas/ListSelector/Main/Group"), true);
            text.transform.SetAsFirstSibling();
            text.rectTransform.localScale = Vector3.one;
            text.rectTransform.localPosition = new Vector3(0, 350, text.rectTransform.localPosition.z);
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                DestroyImmediate(gameObject);
                return;
            }

            //Check if we are on the correct build of ROUNDS (Version 0.1)
            //if (Application.version != "0.1")
            //{

            //    var brokenText = new GameObject("Broken Text", typeof(TextMeshProUGUI), typeof(Canvas)).GetComponent<TextMeshProUGUI>();
            //    brokenText.text =
            //      "The version of ROUNDS you are playing on\r\n" +
            //      "is not currently compatible with mods.\r\n\r\n\r\n" +
            //      "To ensure proper compatability, please go to\r\n" +
            //      "the game's properties by right-clicking on it in steam.\r\n\r\n\r\n" +
            //      "Select the Betas tab, and set the Beta Participation\r\n" +
            //      "to old-rounds-for-mods";
            //    brokenText.fontSize = 2;
            //    brokenText.fontSizeMin = 1;
            //    brokenText.alignment = TextAlignmentOptions.Center;

            //    //Modals dont work on the new version, so just replace the entire MainMenu with a textbox
            //    this.ExecuteAfterFrames(5, () =>
            //    {
            //        MainMenuHandler.instance.transform.parent.gameObject.SetActive(false);
            //    });
            //}




            // Patch game with Harmony
            var harmony = new Harmony(ModId);
            harmony.PatchAll();

            // Add managers
            gameObject.AddComponent<LevelManager>();
            gameObject.AddComponent<CardManager>();

            // Add menu handlers
            gameObject.AddComponent<ToggleLevelMenuHandler>();
            gameObject.AddComponent<ToggleCardsMenuHandler>();

            LoadAssets();
            GameModeManager.Init();

            // fetch card to use as a template for all custom cards
            templateCard = Resources.Load<GameObject>("0 Cards/0. PlainCard").GetComponent<CardInfo>();
            templateCard.allowMultiple = true;
        }

        private void Start()
        {
            // request mod handshake
            NetworkingManager.RegisterEvent(NetworkEventType.StartHandshake, data =>
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    NetworkingManager.RaiseEvent(NetworkEventType.FinishHandshake,
                        GameModeManager.CurrentHandlerID,
                        GameModeManager.CurrentHandler?.Settings);
                }
                else
                {
                    NetworkingManager.RaiseEvent(NetworkEventType.FinishHandshake);
                }
            });

            // receive mod handshake
            NetworkingManager.RegisterEvent(NetworkEventType.FinishHandshake, data =>
            {
                // attempt to syncronize levels and cards with other players
                MapManager.instance.levels = LevelManager.activeLevels.ToArray();

                if (data.Length <= 0) return;
                GameModeManager.SetGameMode((string) data[0], false);
                GameModeManager.CurrentHandler.SetSettings((GameSettings) data[1]);
            });

            CardManager.defaultCards = CardChoice.instance.cards;

            // register default cards with toggle menu
            foreach (var card in CardManager.defaultCards)
            {
                CardManager.cards.Add(card.name,
                    new Card("Vanilla", config.Bind("Cards: Vanilla", card.name, true), card));
            }

            // hook up Photon callbacks
            var networkEvents = gameObject.AddComponent<NetworkEventCallbacks>();
            networkEvents.OnJoinedRoomEvent += OnJoinedRoomAction;
            networkEvents.OnJoinedRoomEvent += LevelManager.OnJoinedRoomAction;
            networkEvents.OnJoinedRoomEvent += CardManager.OnJoinedRoomAction;
            networkEvents.OnLeftRoomEvent += OnLeftRoomAction;
            networkEvents.OnLeftRoomEvent += CardManager.OnLeftRoomAction;
            networkEvents.OnLeftRoomEvent += LevelManager.OnLeftRoomAction;

            // Adds the ping monitor
            gameObject.AddComponent<PingMonitor>();

            // sync modded clients
            networkEvents.OnJoinedRoomEvent += SyncModClients.RequestSync;

            RegisterMenu(ModName, () => { }, Config, null, true);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1) && !ModOptions.noDeprecatedMods)
            {
                ModOptions.showModUi = !ModOptions.showModUi;
            }

            GameManager.lockInput = ModOptions.showModUi ||
                                    DevConsole.isTyping ||
                                    ToggleLevelMenuHandler.instance.mapMenuCanvas.activeInHierarchy ||

                                    (UIHandler.instance.transform.Find("Canvas/EscapeMenu/UIOptions_ESC/Group") &&
                                     UIHandler.instance.transform.Find("Canvas/EscapeMenu/UIOptions_ESC/Group")
                                         .gameObject.activeInHierarchy) ||

                                    (UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Group") &&
                                     UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Group").gameObject
                                         .activeInHierarchy) ||

                                    (
                                    UIHandler.instance.transform.Find("Canvas/EscapeMenu/MODS/Group") &&
                                    UIHandler.instance.transform.Find("Canvas/EscapeMenu/MODS/Group").gameObject.activeInHierarchy) ||
                                    ToggleCardsMenuHandler.menuOpenFromOutside ||
                                    lockInputBools.Values.Any(b => b);
        }

        private void Config(GameObject menu)
        {
            MenuHandler.CreateText($"{ModName} Options", menu, out TextMeshProUGUI _, 45);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 15);
            MenuHandler.CreateToggle(lockMouse.Value, "Lock mouse to the game window (normally the game only does this for exclusive fullscreen)", menu, (value) => { lockMouse.Value = value; Cursor.lockState = lockMouse.Value ? CursorLockMode.Confined : CursorLockMode.None; }, 30);
        }

        private void OnGUI()
        {
            if (!ModOptions.showModUi) return;

            GUILayout.BeginVertical();

            bool showingSpecificMod = false;
            foreach (ModOptions.GUIListener data in ModOptions.GUIListeners.Keys.Select(md => ModOptions.GUIListeners[md]).Where(data => data.guiEnabled))
            {
                if (GUILayout.Button("<- Back"))
                {
                    data.guiEnabled = false;
                }
                GUILayout.Label(data.modName + " Options");
                showingSpecificMod = true;
                data.guiAction?.Invoke();
                break;
            }

            if (showingSpecificMod) return;

            GUILayout.Label("UnboundLib Options\nThis menu is deprecated");

            GUILayout.Label("Mod Options:");
            foreach (var data in ModOptions.GUIListeners.Values)
            {
                if (GUILayout.Button(data.modName))
                {
                    data.guiEnabled = true;
                }
            }
            GUILayout.EndVertical();
        }

        private static void LoadAssets()
        {
            toggleUI = AssetUtils.LoadAssetBundleFromResources("togglemenuui", typeof(ToggleLevelMenuHandler).Assembly);
            linkAssets = AssetUtils.LoadAssetBundleFromResources("unboundlinks", typeof(Unbound).Assembly);
            UIAssets = AssetUtils.LoadAssetBundleFromResources("unboundui", typeof(Unbound).Assembly);

            if (UIAssets != null)
            {
                modalPrefab = UIAssets.LoadAsset<GameObject>("Modal");
                //Instantiate(UIAssets.LoadAsset<GameObject>("Card Toggle Menu"), canvas.transform).AddComponent<CardToggleMenuHandler>();
            }
        }

        private static void OnJoinedRoomAction()
        {
            //if (!PhotonNetwork.OfflineMode)
            //   CardChoice.instance.cards = CardManager.defaultCards;
            NetworkingManager.RaiseEventOthers(NetworkEventType.StartHandshake);

            OnJoinedRoom?.Invoke();
            foreach (var handshake in handShakeActions)
            {
                handshake?.Invoke();
            }
        }
        private static void OnLeftRoomAction()
        {
            OnLeftRoom?.Invoke();
        }

        [UnboundRPC]
        public static void BuildInfoPopup(string message)
        {
            var popup = new GameObject("Info Popup").AddComponent<InfoPopup>();
            popup.rectTransform.SetParent(Instance.canvas.transform);
            popup.Build(message);
        }

        [UnboundRPC]
        public static void BuildModal(string title, string message)
        {
            BuildModal()
                .Title(title)
                .Message(message)
                .Show();
        }
        public static ModalHandler BuildModal()
        {
            return Instantiate(modalPrefab, Instance.canvas.transform).AddComponent<ModalHandler>();
        }
        public static void RegisterCredits(string modName, string[] credits = null, string[] linkTexts = null, string[] linkURLs = null)
        {
            Credits.Instance.RegisterModCredits(new ModCredits(modName, credits, linkTexts, linkURLs));
        }

        public static void RegisterUpdateChecker(string modName, string modVersion, string repoOwner, string repoName)
        {
            UpdateChecker.Instance.RegisterModUpdateChecker(new ModUpdateChecker(modName, modVersion, repoOwner, repoName));
        }

        public static void RegisterMenu(string name, UnityAction buttonAction, Action<GameObject> guiAction, GameObject parent = null)
        {
            ModOptions.instance.RegisterMenu(name, buttonAction, guiAction, parent);
        }

        // ReSharper disable once MethodOverloadWithOptionalParameter
        public static void RegisterMenu(string name, UnityAction buttonAction, Action<GameObject> guiAction, GameObject parent = null, bool showInPauseMenu = false)
        {
            ModOptions.instance.RegisterMenu(name, buttonAction, guiAction, parent, showInPauseMenu);
        }

        public static void RegisterGUI(string modName, Action guiAction)
        {
            ModOptions.RegisterGUI(modName, guiAction);
        }

        public static void RegisterCredits(string modName, string[] credits = null, string linkText = "", string linkURL = "")
        {
            Credits.Instance.RegisterModCredits(new ModCredits(modName, credits, linkText, linkURL));
        }

        public static void RegisterClientSideMod(string GUID)
        {
            SyncModClients.RegisterClientSideMod(GUID);
        }
        public static void AddAllCardsCallback(Action<CardInfo[]> callback)
        {
            CardManager.AddAllCardsCallback(callback);
        }

        public static void RegisterHandshake(string modId, Action callback)
        {
            // register mod handshake network events
            NetworkingManager.RegisterEvent($"ModLoader_{modId}_StartHandshake", (e) =>
            {
                NetworkingManager.RaiseEvent($"ModLoader_{modId}_FinishHandshake");
            });
            NetworkingManager.RegisterEvent($"ModLoader_{modId}_FinishHandshake", (e) =>
            {
                callback?.Invoke();
            });
            handShakeActions.Add(() => NetworkingManager.RaiseEventOthers($"ModLoader_{modId}_StartHandshake"));
        }

        public static bool IsNotPlayingOrConnected()
        {
            return (GameManager.instance && !GameManager.instance.battleOngoing) &&
                   (PhotonNetwork.OfflineMode || !PhotonNetwork.IsConnected);
        }

        internal static ConfigEntry<T> BindConfig<T>(string section, string key, T defaultValue, ConfigDescription configDescription = null)
        {
            return config.Bind(EscapeConfigKey(section), EscapeConfigKey(key), defaultValue, configDescription);
        }

        private static string EscapeConfigKey(string key)
        {
            return key
                .Replace("=", "&eq;")
                .Replace("\n", "&nl;")
                .Replace("\t", "&tab;")
                .Replace("\\", "&esc;")
                .Replace("\"", "&dquot;")
                .Replace("'", "&squot;")
                .Replace("[", "&lsq;")
                .Replace("]", "&rsq;");
        }

        internal static readonly ModCredits modCredits = new ModCredits("UNBOUND", new[]
        {
            "Willis (Creation, design, networking, custom cards, custom maps, and more)",
            "Tilastokeskus (Custom game modes, networking, structure)",
            "Pykess (Custom cards, stability, menus, syncing, extra player colors, disconnect handling, game mode framework)",
            "Ascyst (Quickplay)", "Boss Sloth Inc. (Menus, UI, custom maps, modded lobby syncing)",
            "willuwontu (Custom cards, ping UI)",
            "otDan (UI)",
            "Bknibb (Update to ROUNDS v1.1.2)"
        }, "Github", "https://github.com/Bknibb/UnboundLib");
    }
}
