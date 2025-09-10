using Recognissimo;
using Recognissimo.Components;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace BlackMagicAPI.Modules.Spells;

internal class CustomSpellCommand : MonoBehaviour, ISpellCommand
{
    internal string SpellName { get; set; } = "[]";
    [AllowNull]
    internal SpellData SpellData { get; set; }
    private PlayerInventory? playerInventory { get; set; }
    private SpeechRecognizer? speechRecognizer { get; set; }

    public void Awake()
    {
        Resources.FindObjectsOfTypeAll<VoiceControlListener>()?.First()?.SpellPages?.Add(this);
    }

    public string GetSpellName() => SpellName.ToLower();

    public void ResetVoiceDetect()
    {
        if (speechRecognizer == null)
        {
            if (!TryGetComponent<SpeechRecognizer>(out var sr))
            {
                Debug.LogError("ResetVoiceDetect: SpeechRecognizer component not found");
                return;
            }
            speechRecognizer = sr;
        }


        string spellNameLower = SpellName.ToLower();
        if (!speechRecognizer.Vocabulary.Contains(spellNameLower))
        {
            speechRecognizer.Vocabulary.Add(spellNameLower);
            Debug.Log($"Added spell name to vocabulary: {spellNameLower}");
        }
    }

    public void TryCastSpell()
    {
        if (playerInventory == null)
        {
            if (!Camera.main.transform.parent.TryGetComponent<PlayerInventory>(out var pi))
            {
                Debug.LogError("Failed to find PlayerInventory");
                return;
            }
            playerInventory = pi;
        }

        if (speechRecognizer == null)
        {
            if (!TryGetComponent<SpeechRecognizer>(out var sr))
            {
                Debug.LogError("Failed to find SpeechRecognizer");
                return;
            }
            speechRecognizer = sr;
        }

        if (playerInventory.GetEquippedItemID() == SpellData.Id)
        {
            playerInventory.cPageSpell();
        }

        speechRecognizer.StopProcessing();
        StartCoroutine(CoWaitRestartVoiceDetect());
    }

    private IEnumerator CoWaitRestartVoiceDetect()
    {
        while (speechRecognizer?.State != SpeechProcessorState.Inactive)
        {
            yield return null;
        }
        speechRecognizer?.StartProcessing();
    }
}
