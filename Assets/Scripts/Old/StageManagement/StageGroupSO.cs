using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageGroup", menuName = "Create SO/StageGroup", order = 2)]
public class StageGroupSO : ScriptableObject
{
    #region Serialized Fields
    [SerializeField] List<StageDataSO> _stages;
    #endregion

    #region 외부 전용 반환 메소드
    public List<StageDataSO> stages => _stages;
    #endregion
}
