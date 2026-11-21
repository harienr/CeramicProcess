using UnityEditor;
using UnityEngine;

namespace CiCongNiSheng.EditorTools
{
    public static class JourneyBatchValidation
    {
        public static void Run()
        {
            Debug.Log(JourneyValidationCases.RunFlow());
            JourneyEditorTools.BeginValidation();
        }
    }
}