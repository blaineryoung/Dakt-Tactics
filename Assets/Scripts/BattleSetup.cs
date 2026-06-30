using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    public class BattleSetup : MonoBehaviour
    {
        public Unit unitPrefabTemplate; // assign your Unit prefab here in the Inspector
        public GemDatabase gemDatabase;

        private void Start()
        {
            Unit myUnit = Instantiate(unitPrefabTemplate);
            myUnit.isPlayerControlled = true;
            myUnit.unitName = "Dakt";
            myUnit.PlaceOnTile(GridManager.Instance.GetTile(2, 3));
            GiveSkill(myUnit, "Fireball");
            GiveSkill(myUnit, "ColdSpike");
            GiveSkill(myUnit, "Heal");
            TurnManager.Instance.RegisterUnit(myUnit);

            Unit theirUnit = Instantiate(unitPrefabTemplate);
            theirUnit.isPlayerControlled = false;
            theirUnit.unitName = "Cicero";
            theirUnit.PlaceOnTile(GridManager.Instance.GetTile(6, 3));
            GiveSkill(theirUnit, "Fireball");
            TurnManager.Instance.RegisterUnit(theirUnit);
        }

        void GiveSkill(Unit unit, string skillName)
        {
            ActiveSkillGemData fireball = gemDatabase.FindActive(skillName);
            var group = new GemSocketGroup { socketCount = 4 };
            group.TrySetActiveGem(fireball);
            unit.socketGroups.Add(group);
        }
    }
}