using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine.Localization;
using HarmonyLib;
using System;

namespace UnboundLib.GameModes
{
    /// <inheritdoc/>
    public abstract class GameModeHandler<T> : IGameModeHandler<T> where T : MonoBehaviour
    {
        public T GameMode {
            get
            {
                return GameModeManager.GetGameMode<T>(gameModeId);
            }
        }

        MonoBehaviour IGameModeHandler.GameMode
        {
            get
            {
                return GameMode;
            }
        }

        public abstract GameSettings Settings { get; protected set; }
        public abstract string Name { get; }
        public virtual bool OnlineOnly => false;
        public abstract bool AllowTeams { get; }
        public virtual UISettings UISettings => new UISettings();

        // Used to find the correct game mode from scene
        internal readonly string gameModeId;

        protected GameModeHandler(string gameModeId)
        {
            this.gameModeId = gameModeId;
        }

        public void SetSettings(GameSettings settings)
        {
            Settings = settings;

            foreach (var entry in Settings)
            {
                ChangeSetting(entry.Key, entry.Value);
            }
        }

        public virtual void ChangeSetting(string name, object value)
        {
            var newSettings = new GameSettings();

            foreach (var entry in Settings)
            {
                newSettings.Add(entry.Key, entry.Key == name ? value : entry.Value);
            }

            Settings = newSettings;
        }

        public abstract void PlayerJoined(Player player);

        public abstract void PlayerDied(Player killedPlayer, int playersAlive);

        public virtual void PlayerLeft(Player leftPlayer)
        {
            List<Player> remainingPlayers = PlayerManager.instance.players.Where(p => p != leftPlayer).ToList();
            int playersAlive = remainingPlayers.Count(p => !p.data.dead);

            if (!leftPlayer.data.dead)
            {
                try
                {
                    PlayerDied(leftPlayer, playersAlive);
                }
                catch
                {
                    // ignored
                }
            }

            // get new playerIDs
            Dictionary<Player, int> newPlayerIDs = new Dictionary<Player, int>();
            int playerID = 0;
            foreach (Player player in remainingPlayers.OrderBy(p => p.PlayerID))
            {
                newPlayerIDs[player] = playerID;
                playerID++;
            }

            // fix cardbars by reassigning CardBarHandler.cardBars
            // this leaves the disconnected player(s)' bar unchanged, since removing it can cause issues with other mods
            List<CardBar> cardBars = ((CardBar[]) CardBarHandler.instance.GetFieldValue("cardBars")).ToList();
            List<CardBar> newCardBars = new List<CardBar>();
            newCardBars.AddRange(
                from p in newPlayerIDs.Keys
                orderby newPlayerIDs[p]
                select cardBars[p.PlayerID]
            );
            CardBarHandler.instance.SetFieldValue("cardBars", newCardBars.ToArray());

            // reassign playerIDs
            foreach (Player player in newPlayerIDs.Keys)
            {
                player.AssignPlayerID(newPlayerIDs[player]);
            }

            // reassign teamIDs
            Dictionary<int, List<Player>> teams = new Dictionary<int, List<Player>>();
            foreach (Player player in remainingPlayers.OrderBy(p=>p.TeamID).ThenBy(p=>p.PlayerID))
            {
                if (!teams.ContainsKey(player.TeamID)) { teams[player.TeamID] = new List<Player>() { }; }

                teams[player.TeamID].Add(player);
            }

            int teamID = 0;
            foreach (int oldID in teams.Keys)
            {
                foreach (Player player in teams[oldID])
                {
                    player.AssignTeamID(teamID);
                }
                teamID++;
            }

            PlayerManager.instance.players = remainingPlayers.ToList();

            // count number of unique teams remaining as well as the number of unique clients, if either are equal to 1, the game is borked
            if (GameManager.instance.isPlaying && (PlayerManager.instance.players.Select(p => p.TeamID).Distinct().Count() <= 1 || PlayerManager.instance.players.Select(p => p.data.view.ControllerActorNr).Distinct().Count() <= 1))
            {
                Unbound.Instance.StartCoroutine((IEnumerator) AccessTools.Method(typeof(NetworkConnectionHandler), "DoDisconnect", new Type[] { typeof(LocalizedString), typeof(string) }).Invoke(NetworkConnectionHandler.instance, new object[] { NetworkConnectionHandler.instance.GetFieldValue("m_localizedDisconnect"), "TOO MANY DISCONNECTS" }));
            }
        }

        public abstract TeamScore GetTeamScore(int teamID);

        public abstract void SetTeamScore(int teamID, TeamScore score);

        public abstract void SetActive(bool active);

        public abstract void StartGame();

        public abstract void ResetGame();
        public virtual int[] GetGameWinners()
        {
            return new int[] { };
        }
        public virtual int[] GetRoundWinners()
        {
            return new int[] { };
        }
        public virtual int[] GetPointWinners()
        {
            return new int[] { };
        }
    }
}
