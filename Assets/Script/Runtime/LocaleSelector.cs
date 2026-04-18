using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class LocaleSelector : MonoBehaviour
{
    private bool active = false;

    public void ChangeLocale(int localeID)
    {
        if (active)
            return;

        StartCoroutine(SetLocale(localeID));
    }

    private IEnumerator SetLocale(int localeID)
    {
        active = true;

        yield return LocalizationSettings.InitializationOperation;

        LocalizationSettings.SelectedLocale =
            LocalizationSettings.AvailableLocales.Locales[localeID];

        active = false;
    }
}