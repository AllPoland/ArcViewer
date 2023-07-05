using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SoupInput : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI placeholderText;
    [SerializeField] private string soupPlaceholder;

    [Space]
    [SerializeField] private List<LockedLog> lockedLogs;

    private string defaultPlaceholder;


    public bool CheckSoupWord(string input)
    {
        if(!SettingsManager.GetBool(TheSoup.Rule))
        {
            return false;
        }

        foreach(LockedLog lockedLog in lockedLogs)
        {
            if(string.Equals(input, lockedLog.Passcode, StringComparison.InvariantCultureIgnoreCase))
            {
                DialogueHandler.Instance.ShowLog(lockedLog.Log);
                return true;
            }
        }
        return false;
    }


    private void UpdatePlaceholderText()
    {
        bool soup = SettingsManager.GetBool(TheSoup.Rule);
        placeholderText.text = soup ? soupPlaceholder : defaultPlaceholder;
    }


    public void UpdateSettings(string changedSetting)
    {
        if(changedSetting == "all" || changedSetting == TheSoup.Rule)
        {
            UpdatePlaceholderText();
        }
    }


    private void OnEnable()
    {
        SettingsManager.OnSettingsUpdated += UpdateSettings;

        if(string.IsNullOrEmpty(defaultPlaceholder))
        {
            defaultPlaceholder = placeholderText.text;
        }

        if(SettingsManager.Loaded)
        {
            UpdatePlaceholderText();
        }
    }


    private void OnDisable()
    {
        SettingsManager.OnSettingsUpdated -= UpdateSettings;
    }


    [Serializable]
    public class LockedLog
    {
        public string Passcode;
        public Log Log;
    }
}