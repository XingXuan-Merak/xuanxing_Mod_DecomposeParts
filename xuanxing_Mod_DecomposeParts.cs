using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace xuanxing_Mod_DecomposeParts
{
    [BepInPlugin("xuanxing_Mod_DecomposeParts", "DecomposeParts", "1.0.0")]
    [HarmonyPatch]
    public class DecomposeParts : BaseUnityPlugin
    {
        //启动时加载补丁
        private void Awake()
        {
            new Harmony("xuanxing_Mod_DecomposeParts").PatchAll();
            UnityEngine.Debug.Log("【块部件分解mod】执行：PatchAll");
        }
        [HarmonyPatch(typeof(CBlockPartsSelectLayer), "OnClick_partsInner")]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);
            int startIndex = -1;
            int endIndex = -1;
            for (int i = 0; i < codes.Count - 1; i++)
            {
                UnityEngine.Debug.Log("【块部件分解mod】执行：for，次数" + i.ToString());
                //搜索call  class CItemManager CItemManager::get_instance()
                //第二行应为callvirt  instance class [netstandard]System.Collections.Generic.List`1<class CBlockParts> CItemManager::get_m_hasBlockPartsList()
                //之后应存在callvirt  instance void class [netstandard]System.Collections.Generic.List`1<class CBlockParts>::Add(!0)
                if (codes[i].opcode == OpCodes.Call && codes[i].operand is MethodInfo methodInfo1 && methodInfo1.Name == "get_instance" && methodInfo1.DeclaringType?.Name == "CItemManager" &&
                    codes[i + 1].opcode == OpCodes.Callvirt && codes[i + 1].operand is MethodInfo methodInfo2 && methodInfo2.Name.Contains("get_m_hasBlockPartsList"))
                {
                    for (int j = i; j < Math.Min(i + 30, codes.Count); j++)
                    {
                        if (codes[j].opcode == OpCodes.Callvirt && codes[j].operand is MethodInfo methodInfo3 && methodInfo3.Name == "Add")
                        {
                            startIndex = i;
                            endIndex = j;
                            break;
                        }
                    }
                    if (endIndex != -1) break;
                }
            }
            if (endIndex != -1)
            {
                UnityEngine.Debug.Log("【块部件分解mod】执行：Transpiler");
                var newCodes = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CBlockPartsSelectLayer).GetNestedType("<>c__DisplayClass33_0", BindingFlags.NonPublic), "view")),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DecomposeParts), "DecomposeAndAddParts"))
                };
                codes.RemoveRange(startIndex, endIndex - startIndex + 1);
                codes.InsertRange(startIndex, newCodes);
            }
            UnityEngine.Debug.Log("【块部件分解mod】成功");
            return codes;
        }
        static void DecomposeAndAddParts(ViewSelectBlockPartsInTresure view)
        {
            foreach (COnePanelData conePanelData in view.m_blockParts.m_innerPanelList)
            {
                CItemManager.TYPE_PARTS partsType = conePanelData.m_partsType;
                CItemManager.instance.m_havePartsNumArr[(int)partsType]++;
            }
            CItemManager.instance.UpdatePartsMoneyUIList();
        }
    }
}
