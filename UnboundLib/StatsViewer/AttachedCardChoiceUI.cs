using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UnboundLib.StatsViewer
{
    class AttachedCardChoiceUI : MonoBehaviour
    {
        public static AttachedCardChoiceUI instance;
        public void Awake()
        {
            instance = this;
            StatsViewer.Init();
        }
        public void Update()
        {
            StatsViewer.UpdateControls();
        }
    }
}
