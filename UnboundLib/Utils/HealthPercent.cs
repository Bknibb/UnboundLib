using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace UnboundLib.Utils
{
    class HealthPercent : MonoBehaviour
    {
        private CharacterData characterData;
        private TextMeshProUGUI text;
        private void Start()
        {
            characterData = GetComponentInParent<CharacterData>();
            text = GetComponent<TextMeshProUGUI>();
        }
        private void Update()
        {
            text.text = Mathf.RoundToInt((characterData.health / characterData.MaxHealth) * 100) + "%";
        }
    }
}
