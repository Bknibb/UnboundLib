using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

namespace UnboundLib.Utils.UI
{
    public class UpdateChecker
    {
        internal Dictionary<string, ModUpdateChecker> modUpdateCheckers = new Dictionary<string, ModUpdateChecker>();

        internal Dictionary<string, ModUpdateChecker> modsWithUpdates = new Dictionary<string, ModUpdateChecker>();

        private bool firstTime = true;

        public static UpdateChecker Instance = new UpdateChecker();

        private GameObject UpdatesMenu;

        private UpdateChecker()
        {
            // singleton first time setup
            Instance = this;
        }

        internal void RegisterModUpdateChecker(ModUpdateChecker modUpdateChecker)
        {
            this.modUpdateCheckers[modUpdateChecker.modName] = modUpdateChecker;
            GithubUpdateChecker.CheckForUpdates(modUpdateChecker.repoOwner, modUpdateChecker.repoName, modUpdateChecker.currentVersion)
                .ContinueWith(task =>
                {
                    if (task.IsCompleted && task.Result)
                    {
                        modsWithUpdates[modUpdateChecker.modName] = modUpdateChecker;
                        CreateUpdateMenu(modUpdateChecker);
                    }
                });
        }

        internal void CreateUpdateMenu(ModUpdateChecker modUpdateChecker)
        {
            Unbound.Instance.ExecuteAfterSeconds(firstTime ? 0.1f : 0f, () =>
            {
                firstTime = false;

                if (UpdatesMenu == null)
                {
                    UpdatesMenu = new GameObject("UpdatesMenu");
                    UpdatesMenu.transform.SetParent(MainMenuHandler.instance.transform.Find("Canvas"), true);
                    UpdatesMenu.transform.SetAsFirstSibling();
                    UpdatesMenu.transform.localScale = Vector3.one;
                    float targetHeight16by9 = Screen.width * 9f / 16f;
                    float extraVerticalPixels = Screen.height - targetHeight16by9;
                    float verticalPadding = Mathf.Max(extraVerticalPixels / 2f);
                    UpdatesMenu.transform.position = MainCam.instance.transform.GetComponent<Camera>().ScreenToWorldPoint(new Vector3(0, verticalPadding, 0f));
                    UpdatesMenu.transform.position += new Vector3(0, 0, 100f);
                    UpdatesMenu.transform.localPosition += new Vector3(50f, 50f, 0f);
                    //var verticalLayoutGroup = UpdatesMenu.AddComponent<VerticalLayoutGroup>();
                    //verticalLayoutGroup.padding = new RectOffset(75, 0, 0, 75);
                }

                var text = MenuHandler.CreateTextAt($"{modUpdateChecker.modName} has an update available!", Vector2.zero);
                var link = text.gameObject.AddComponent<Link>();
                link._Links = $"https://github.com/{modUpdateChecker.repoOwner}/{modUpdateChecker.repoName}";
                text.fontSize = 50;
                text.color = (Color.yellow + Color.red) / 2;
                text.alignment = TextAlignmentOptions.BottomLeft;
                text.transform.SetParent(UpdatesMenu.transform);
                text.transform.SetAsFirstSibling();
                text.rectTransform.localScale = Vector3.one;
                text.rectTransform.localPosition = new Vector3(0, 75 * (UpdatesMenu.transform.childCount - 1), text.rectTransform.localPosition.z);
                text.ForceMeshUpdate();
                text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, text.preferredWidth);
                text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, text.preferredHeight);
                text.rectTransform.localPosition += new Vector3(text.rectTransform.rect.width/2, 0, 0);
            });
        }


        public class ModUpdateChecker
        {
            public string modName;
            public string currentVersion;
            public string repoOwner;
            public string repoName;
            public ModUpdateChecker(string modName, string currentVersion, string repoOwner, string repoName)
            {
                this.modName = modName;
                this.currentVersion = currentVersion;
                this.repoOwner = repoOwner;
                this.repoName = repoName;
            }
        }
    }
}
