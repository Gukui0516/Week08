using UnityEngine;
using System.Collections;
[System.Serializable]
public class Cloud
{
    public GameObject cloudGameObject;
    public int weight;
}
public class CloudGenerator : MonoBehaviour
{
    [SerializeField] Cloud[] cloudGameobjects;
    [SerializeField] GameObject cloudContainer;
    [SerializeField] int genStart = 20;//시작시 생성
    [SerializeField] int genMax = 20;//최대 생성 수
    [SerializeField] int genPosXmin = -200;
    [SerializeField] int genPosXmax = 200;
    [SerializeField] int genPosZmin = -200;
    [SerializeField] int genPosZmax = 200;
    [SerializeField] int genPosYmin = 20;
    [SerializeField] int genPosYmax = 25;
    [SerializeField] float genCoolmin = 20;
    [SerializeField] float genCoolmax = 25;
    [SerializeField] bool canGenerate = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < genStart; i++)
        {
            CloudGenerateRandomPos();
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (canGenerate && genStart < genMax)
        {
            CloudGenerateCooltime();

        }
    }
    IEnumerator CloudGenerateCooltime()
    {
        CloudGenerateStartPos();
        float genCool = Random.Range(genCoolmin, genCoolmax);
        yield return new WaitForSeconds(genCool);
        canGenerate = true;
    }
    void CloudGenerateRandomPos()//첨부터 랜덤위치
    {
        int randomObj = Random.Range(0, cloudGameobjects.Length);
        float randomX = Random.Range(genPosXmin, genPosXmax);
        float randomY = Random.Range(genPosYmin, genPosYmax);
        float randomZ = Random.Range(genPosZmin, genPosZmax);

        if(cloudContainer == null)
        {
            cloudContainer = new GameObject("Clounds");
        }
        Instantiate(cloudGameobjects[randomObj].cloudGameObject, new Vector3(randomX, randomY, randomZ), Quaternion.identity, cloudContainer.transform);
    }
    void CloudGenerateStartPos()//z축 고정 랜덤위치
    {
        int randomObj = Random.Range(0, cloudGameobjects.Length);
        float randomX = Random.Range(genPosXmin, genPosXmax);
        float randomY = Random.Range(genPosYmin, genPosYmax);
        CloudMove cloudMove = Instantiate(cloudGameobjects[randomObj].cloudGameObject, new Vector3(randomX, randomY, genPosZmin), Quaternion.identity, cloudContainer.transform).GetComponent<CloudMove>();
        cloudMove.genPosXmin = genPosXmin;
        cloudMove.genPosXmax = genPosXmax;
        cloudMove.genPosYmin = genPosYmin;
        cloudMove.genPosYmax = genPosYmax;
        cloudMove.genPosZmin = genPosZmin;
        cloudMove.genPosZmax = genPosZmax;
    }
}