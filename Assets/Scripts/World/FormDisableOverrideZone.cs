using System.Collections.Generic;
using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[DisallowMultipleComponent]
public class FormDisableOverrideZone : MonoBehaviour
{
    [Header("Overlay Rule")]
    [LabelText("额外禁用的形态")]
    [SerializeField] private List<PlayerFormType> disabledForms = new List<PlayerFormType>();

    public IReadOnlyList<PlayerFormType> DisabledForms => disabledForms;

    public bool DisablesForm(PlayerFormType formType)
    {
        return disabledForms != null && disabledForms.Contains(formType);
    }
}
