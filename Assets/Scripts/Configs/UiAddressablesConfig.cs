using System;
using System.Collections.Generic;
using UI.Popups;
using UI.Screens;
using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(menuName = "CKR/UI Addressables Config", fileName = "UIAddressablesConfig")]
    public sealed class UiAddressablesConfig : ScriptableObject
    {
        [SerializeField] private string _navigationPanelKey;
        [SerializeField] private List<ScreenAddressEntry> _screenEntries = new();
        [SerializeField] private List<PopupAddressEntry> _popupEntries = new();

        public string NavigationPanelKey => _navigationPanelKey;

        public bool TryGetScreenKey(ScreenId screenId, out string key)
        {
            for (var i = 0; i < _screenEntries.Count; i++)
            {
                var entry = _screenEntries[i];
                if (entry.ScreenId != screenId)
                {
                    continue;
                }

                key = entry.AddressableKey;
                return !string.IsNullOrWhiteSpace(key);
            }

            key = null;
            return false;
        }

        public bool TryGetPopupKey(PopupId popupId, out string key)
        {
            for (var i = 0; i < _popupEntries.Count; i++)
            {
                var entry = _popupEntries[i];
                if (entry.PopupId != popupId)
                {
                    continue;
                }

                key = entry.AddressableKey;
                return !string.IsNullOrWhiteSpace(key);
            }

            key = null;
            return false;
        }

        [Serializable]
        private struct ScreenAddressEntry
        {
            public ScreenId ScreenId;
            public string AddressableKey;
        }

        [Serializable]
        private struct PopupAddressEntry
        {
            public PopupId PopupId;
            public string AddressableKey;
        }
    }
}
