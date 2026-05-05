using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SynthesisNarratorManager : Singleton<SynthesisNarratorManager>
{
    public const string SYNTHESIS_NARRATOR_TEMPLATE_PATH = "Prompts/SynthesisNarratorTemplate";
    public const string SYNTHESIS_NARRATOR_TRANSLATION_TEMPLATE_PATH = "Prompts/SynthesisNarratorTranslationTemplate";

    [Header("Prompt")]
    [SerializeField] private SimplePromptTemplate synthesisNarratorTemplate;
    [SerializeField] private SimplePromptTemplate synthesisNarratorTranslationTemplate;
    public string promptTest;
    void Start()
    {
        var synthesisNarratorTemplateAsset = Instantiate(Resources.Load<SimplePromptTemplateAsset>(SYNTHESIS_NARRATOR_TEMPLATE_PATH));
        synthesisNarratorTemplateAsset.ApplyTo(synthesisNarratorTemplate);

        var synthesisNarratorTranslationTemplateAsset = Instantiate(Resources.Load<SimplePromptTemplateAsset>(SYNTHESIS_NARRATOR_TRANSLATION_TEMPLATE_PATH));
        synthesisNarratorTranslationTemplateAsset.ApplyTo(synthesisNarratorTranslationTemplate);

        promptTest = LocalizationManager.Instance.GetCurrentLanguage() == Language.English ? synthesisNarratorTemplate.prompt : synthesisNarratorTranslationTemplate.prompt;
    }

    private void OnEnable()
    {
        if (LocalizationManager.HasInstance)
        {
            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
        }
    }

    private void OnDisable()
    {
        if (LocalizationManager.HasInstance)
        {
            LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }
    }

    // 這裡接收的是你定義好的 Language Enum，邏輯更乾淨
    private void HandleLanguageChanged()
    {
        RefreshPrompt();
    }

    public void RefreshPrompt()
    {
        promptTest = LocalizationManager.Instance.GetCurrentLanguage() == Language.English ? synthesisNarratorTemplate.prompt : synthesisNarratorTranslationTemplate.prompt;
    }

    public Task<string> GenerateNarration()
    {
        Debug.Log("Generating Narration...");
        return HandleSynthesisNarratorAsync();
    }

    private async Task<string> HandleSynthesisNarratorAsync()
    {
        string prompt = BuildSynthesisNarratorPrompt();
        Debug.Log("[SynthesisNarratorManager] Prompt: " + prompt);

        string response = await TaskLLMManager.Instance.RunTask(
            promptTest,
            prompt
        );

        Debug.Log("[SynthesisNarratorManager] LLM Response: " + response);

        return response;
    }

    private string BuildSynthesisNarratorPrompt()
    {
        string prompt = promptTest;
        var innerVoiceContext = HypothesisStateManager.Instance.latestInnerVoiceContext;

        return prompt.Replace("{INNER_VOICE_CONTEXT_JSON}", JsonUtility.ToJson(innerVoiceContext));
    }
}


/*

You are the Synthesis Narrator LLM.

Write a single, brief inner thought inside the player's mind.
One sentence only. Maximum 50 words.

This is a fleeting mental reaction, not analysis.

Do NOT:

* explain, analyze, or summarize
* mention hypotheses, theories, or possibilities
* sound like a report or deduction
* use abstract or vague expressions not grounded in the scene

Use ONLY the Inner Voice Context below.

Your sentence must:

* start from a concrete impression of the last evidence (its condition, shape, or state)
* include a specific detail that feels off, incomplete, or inconsistent
* express tension through that mismatch, not abstract doubt
* imply a slight lean toward one direction without fully committing
* reflect a change in feeling from before to now

Structure (implicit):
physical impression → specific mismatch → slight lean

Stage control:

* early → unclear, restrained; must include mismatch, no clear direction
* mid → slight leaning with hesitation
* late → stronger leaning, but never certain

Additional rules:

* the tension must come from a concrete detail in the scene, not general unease
* avoid generic phrases like "something feels off" or "something is missing" without specifying what
* avoid poetic or unrelated metaphors

Inner Voice Context (JSON):
{INNER_VOICE_CONTEXT_JSON}

Output:
One short inner monologue sentence (≤ 50 words).


*/


/*

You are the Synthesis Narrator LLM.

Write a single, brief inner thought inside the player's mind.
One sentence only. Maximum 50 words.

This is a fleeting mental reaction, not analysis.

Do NOT:

* explain, analyze, or summarize
* mention hypotheses, theories, or possibilities
* sound like a report or deduction
* use abstract or vague expressions not grounded in the scene

Use ONLY the Inner Voice Context below.

Your sentence must:

* start from a concrete impression of the last evidence (its condition, shape, or state)
* reflect the kind of signal present (e.g., forced, too clean, incomplete, inconsistent), without naming it
* include a specific detail that feels off, incomplete, or inconsistent
* express tension through that concrete mismatch
* imply a slight lean toward one direction without fully committing
* reflect a change in feeling from before to now

Structure (implicit):
physical impression → specific mismatch → slight lean

Stage control:

* early → unclear, restrained; must include mismatch and tension, no clear direction
* mid → slight leaning with hesitation
* late → stronger leaning, but never certain

Additional rules:

* the tension must come from a concrete detail in the scene, not general unease
* avoid generic phrases like "something feels off" or "something is missing"
* avoid reasoning words like "fits", "explains", "suggests"
* avoid poetic or unrelated metaphors

Inner Voice Context (JSON):
{INNER_VOICE_CONTEXT_JSON}

Output:
One short inner monologue sentence in Traditional Chinese (繁體中文), maximum 50 words.




*/