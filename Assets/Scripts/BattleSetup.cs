using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    public class BattleSetup : MonoBehaviour
    {
        public Unit unitPrefabTemplate; // assign your Unit prefab here in the Inspector

        private void Start()
        {
            Unit myUnit = Instantiate(unitPrefabTemplate);
            myUnit.isPlayerControlled = true;
            myUnit.unitName = "Dakt";
            myUnit.PlaceOnTile(GridManager.Instance.GetTile(2, 3));
            TurnManager.Instance.RegisterUnit(myUnit);

            Unit theirUnit = Instantiate(unitPrefabTemplate);
            theirUnit.isPlayerControlled = false;
            theirUnit.unitName = "Cicero";
            theirUnit.PlaceOnTile(GridManager.Instance.GetTile(6, 3));
            TurnManager.Instance.RegisterUnit(theirUnit);
        }
    }
}