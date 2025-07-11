// �����Ǹ� ���� Extension ����

using System;
using UnityEngine.Events;

namespace UnityEngine.UI.Extensions
{
    public static class UIExtension
    {
        public static void SetBtnListnerRemoveAllAndAdd(this Button btn, UnityAction callback)
        {
            if (btn == null)
            {
#if UNITY_EDITOR
                Debug.LogError("btn is null");
#endif
                return;
            }
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(callback);
        }
    }
}
