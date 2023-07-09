using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SoupInput : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI placeholderText;
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
            if(input.Equals(lockedLog.Passcode, StringComparison.InvariantCultureIgnoreCase))
            {
                Debug.Log("smil");
                DialogueHandler.Instance.ShowLog(lockedLog.Log);
                return true;
            }
        }
        return false;
    }



    private void OnEnable()
    {
        if(string.IsNullOrEmpty(defaultPlaceholder))
        {
            defaultPlaceholder = placeholderText.text;
        }
    }


    [Serializable]
    public class LockedLog
    {
        public string Passcode;
        public Log Log;
    }
}