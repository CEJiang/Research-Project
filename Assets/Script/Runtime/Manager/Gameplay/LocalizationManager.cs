using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class LocalizationManager : Singleton<LocalizationManager>
{
    public delegate void LanguageChangedHandler();
    public event LanguageChangedHandler OnLanguageChanged;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
    }

    private void HandleLocaleChanged(UnityEngine.Localization.Locale _)
    {
        OnLanguageChanged?.Invoke();
    }

    public Language GetCurrentLanguage()
    {
        string code = LocalizationSettings.SelectedLocale.Identifier.Code;
        return code.Contains("zh-Hant") ? Language.Chinese : Language.English;
    }
}