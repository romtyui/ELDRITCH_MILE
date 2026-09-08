using UnityEngine;

namespace EldritchMile.Map3D
{
    /// <summary>
    /// 只繞 Y 軸面向相機。**3D 地圖上的棋子靠這個站著。**
    ///
    /// 【為什麼不能用完整 billboard】
    /// 完整 billboard 會讓物件的法線正對相機 —— 相機一往下俯視，
    /// 棋子就跟著往後躺，看起來像貼在地上的貼紙，不是插在地圖上的針。
    ///
    /// 只鎖 Y 軸的話棋子永遠是直立的，只是左右轉身面對你。
    /// 相機繞著轉的時候，你會看到它「轉過來」而不是「倒下去」——
    /// 那個差別就是「像棋子」與「像貼紙」的分界。
    ///
    /// 【底座的圓環不要掛這支】
    /// 圓環要躺在地面上跟著透視變形，那才是 3D 感的來源。
    /// 掛了這支它就會站起來，整個效果就沒了。
    /// </summary>
    [ExecuteAlways]
    public class YBillboard : MonoBehaviour
    {
        [Tooltip("要面向的相機。留空則用 Camera.main")]
        public Camera target;

        private void LateUpdate()
        {
            // MonoBehaviour 本身就有 runInEditMode 這個屬性，別再宣告一個同名的
            // （會蓋掉內建的，而且 CS0108 警告會一直跟著）。
            // 編輯模式要不要跑交給 [ExecuteAlways] 決定就好
            Camera cam = target != null ? target : Camera.main;
            if (cam == null) return;

            // 只取相機在水平面上的朝向 —— y 歸零就是「不管相機俯角多少」
            Vector3 fwd = cam.transform.forward;
            fwd.y = 0f;

            // 相機正上方俯視時 fwd 會趨近零向量，那時不要轉（轉了會亂跳）
            if (fwd.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }
    }
}
