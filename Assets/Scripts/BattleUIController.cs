using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// Bare-bones battle UI: shows the active unit's name/HP/MP and a preview of
/// upcoming turn order. Wire the TextMeshProUGUI fields in the Inspector to
/// a Canvas you build in the scene. Replace with proper UI/UX later.
/// </summary>
public class BattleUIController : MonoBehaviour
{
    public TextMeshProUGUI activeUnitText;
    public TextMeshProUGUI turnOrderText;

    private void Start()
    {
        TurnManager.Instance.OnUnitTurnStart += UpdateActiveUnitDisplay;
    }

    private void UpdateActiveUnitDisplay(Unit unit)
    {
        if (activeUnitText != null)
        {
            activeUnitText.text = $"{unit.unitName}\nHP: {unit.currentHP}/{unit.maxHP}  MP: {unit.currentMP}/{unit.maxMP}";
        }

        if (turnOrderText != null)
        {
            var upcoming = TurnManager.Instance.PreviewTurnOrder(5);
            turnOrderText.text = "Turn Order: " + string.Join(" -> ", upcoming.Select(u => u.ToString()));
        }
    }

    // Hook this to an "End Turn" Button OnClick in the Inspector.
    public void OnEndTurnButtonPressed()
    {
        FindObjectOfType<BattleController>()?.EndTurn();
    }
}
